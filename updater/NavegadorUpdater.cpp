#define UNICODE
#define _UNICODE
#include <windows.h>
#include <winhttp.h>
#include <bcrypt.h>
#include <shellapi.h>
#include <filesystem>
#include <fstream>
#include <iomanip>
#include <sstream>
#include <string>
#include <vector>
#include <algorithm>
#include <cctype>

#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "bcrypt.lib")
#pragma comment(lib, "shell32.lib")

static constexpr wchar_t kManifestUrl[] =
    L"https://github.com/joaoldsxyzbr/Navegador/releases/latest/download/update.json";

static HWND g_status = nullptr;
static HWND g_button = nullptr;

static std::filesystem::path ModuleDirectory() {
    std::vector<wchar_t> buffer(32768);
    DWORD size = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
    if (size == 0 || size >= buffer.size()) return {};
    return std::filesystem::path(std::wstring(buffer.data(), size)).parent_path();
}

static std::string ReadUtf8(const std::filesystem::path& path) {
    std::ifstream file(path, std::ios::binary);
    return std::string(std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>());
}

static std::wstring Utf8ToWide(const std::string& value) {
    if (value.empty()) return {};
    int size = MultiByteToWideChar(CP_UTF8, 0, value.data(), static_cast<int>(value.size()), nullptr, 0);
    std::wstring out(size, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, value.data(), static_cast<int>(value.size()), out.data(), size);
    return out;
}

static std::string JsonString(const std::string& json, const std::string& key) {
    const std::string needle = "\"" + key + "\"";
    auto pos = json.find(needle);
    if (pos == std::string::npos) return {};
    pos = json.find(':', pos + needle.size());
    if (pos == std::string::npos) return {};
    pos = json.find('"', pos + 1);
    if (pos == std::string::npos) return {};

    std::string out;
    bool escape = false;
    for (++pos; pos < json.size(); ++pos) {
        const char ch = json[pos];
        if (escape) {
            switch (ch) {
                case '"': out.push_back('"'); break;
                case '\\': out.push_back('\\'); break;
                case '/': out.push_back('/'); break;
                case 'n': out.push_back('\n'); break;
                case 'r': out.push_back('\r'); break;
                case 't': out.push_back('\t'); break;
                default: out.push_back(ch); break;
            }
            escape = false;
            continue;
        }
        if (ch == '\\') { escape = true; continue; }
        if (ch == '"') return out;
        out.push_back(ch);
    }
    return {};
}

static std::vector<int> VersionParts(const std::string& version) {
    std::vector<int> parts;
    std::string token;
    for (char ch : version) {
        if (ch == '.') {
            parts.push_back(token.empty() ? 0 : std::stoi(token));
            token.clear();
        } else if (ch >= '0' && ch <= '9') {
            token.push_back(ch);
        } else {
            break;
        }
    }
    if (!token.empty()) parts.push_back(std::stoi(token));
    return parts;
}

static bool IsNewerVersion(const std::string& candidate, const std::string& current) {
    auto a = VersionParts(candidate);
    auto b = VersionParts(current);
    const size_t count = (std::max)(a.size(), b.size());
    a.resize(count, 0);
    b.resize(count, 0);
    for (size_t i = 0; i < count; ++i) {
        if (a[i] != b[i]) return a[i] > b[i];
    }
    return false;
}

static bool DownloadFile(const std::wstring& url, const std::filesystem::path& destination, std::wstring& error) {
    URL_COMPONENTSW parts{};
    parts.dwStructSize = sizeof(parts);
    wchar_t host[512]{};
    wchar_t path[4096]{};
    wchar_t extra[4096]{};
    parts.lpszHostName = host;
    parts.dwHostNameLength = _countof(host);
    parts.lpszUrlPath = path;
    parts.dwUrlPathLength = _countof(path);
    parts.lpszExtraInfo = extra;
    parts.dwExtraInfoLength = _countof(extra);

    if (!WinHttpCrackUrl(url.c_str(), 0, 0, &parts)) {
        error = L"URL inválida.";
        return false;
    }

    HINTERNET session = WinHttpOpen(L"NavegadorUpdater/1.0",
        WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
        WINHTTP_NO_PROXY_NAME,
        WINHTTP_NO_PROXY_BYPASS,
        0);
    if (!session) {
        error = L"Falha ao iniciar WinHTTP.";
        return false;
    }

    std::wstring hostName(parts.lpszHostName, parts.dwHostNameLength);
    HINTERNET connect = WinHttpConnect(session, hostName.c_str(), parts.nPort, 0);
    if (!connect) {
        WinHttpCloseHandle(session);
        error = L"Falha ao conectar ao servidor.";
        return false;
    }

    std::wstring resource(parts.lpszUrlPath, parts.dwUrlPathLength);
    if (parts.dwExtraInfoLength > 0) {
        resource.append(parts.lpszExtraInfo, parts.dwExtraInfoLength);
    }

    const DWORD flags = parts.nScheme == INTERNET_SCHEME_HTTPS ? WINHTTP_FLAG_SECURE : 0;
    HINTERNET request = WinHttpOpenRequest(connect, L"GET", resource.c_str(), nullptr,
        WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES, flags);

    if (!request) {
        WinHttpCloseHandle(connect);
        WinHttpCloseHandle(session);
        error = L"Falha ao criar a requisição.";
        return false;
    }

    bool ok = false;
    HANDLE file = INVALID_HANDLE_VALUE;

    do {
        if (!WinHttpSendRequest(request, WINHTTP_NO_ADDITIONAL_HEADERS, 0,
                                WINHTTP_NO_REQUEST_DATA, 0, 0, 0)) {
            error = L"Falha ao enviar a requisição.";
            break;
        }

        if (!WinHttpReceiveResponse(request, nullptr)) {
            error = L"Falha ao receber a resposta.";
            break;
        }

        DWORD status = 0;
        DWORD statusSize = sizeof(status);
        if (!WinHttpQueryHeaders(request,
                WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
                WINHTTP_HEADER_NAME_BY_INDEX,
                &status,
                &statusSize,
                WINHTTP_NO_HEADER_INDEX) || status < 200 || status >= 300) {
            error = L"O servidor respondeu com status HTTP " + std::to_wstring(status) + L".";
            break;
        }

        file = CreateFileW(destination.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS,
                           FILE_ATTRIBUTE_NORMAL, nullptr);
        if (file == INVALID_HANDLE_VALUE) {
            error = L"Não foi possível criar o arquivo baixado.";
            break;
        }

        std::vector<unsigned char> buffer(128 * 1024);
        for (;;) {
            DWORD read = 0;
            if (!WinHttpReadData(request, buffer.data(), static_cast<DWORD>(buffer.size()), &read)) {
                error = L"Falha durante o download.";
                break;
            }
            if (read == 0) {
                ok = true;
                break;
            }

            DWORD written = 0;
            if (!WriteFile(file, buffer.data(), read, &written, nullptr) || written != read) {
                error = L"Falha ao gravar o download.";
                break;
            }
        }
    } while (false);

    if (file != INVALID_HANDLE_VALUE) CloseHandle(file);
    WinHttpCloseHandle(request);
    WinHttpCloseHandle(connect);
    WinHttpCloseHandle(session);

    if (!ok) DeleteFileW(destination.c_str());
    return ok;
}

static std::string Sha256(const std::filesystem::path& path) {
    BCRYPT_ALG_HANDLE algorithm = nullptr;
    BCRYPT_HASH_HANDLE hash = nullptr;
    DWORD objectLength = 0;
    DWORD hashLength = 0;
    DWORD cb = 0;

    if (BCryptOpenAlgorithmProvider(&algorithm, BCRYPT_SHA256_ALGORITHM, nullptr, 0) != 0) return {};
    if (BCryptGetProperty(algorithm, BCRYPT_OBJECT_LENGTH,
            reinterpret_cast<PUCHAR>(&objectLength), sizeof(objectLength), &cb, 0) != 0 ||
        BCryptGetProperty(algorithm, BCRYPT_HASH_LENGTH,
            reinterpret_cast<PUCHAR>(&hashLength), sizeof(hashLength), &cb, 0) != 0) {
        BCryptCloseAlgorithmProvider(algorithm, 0);
        return {};
    }

    std::vector<unsigned char> object(objectLength);
    std::vector<unsigned char> digest(hashLength);

    if (BCryptCreateHash(algorithm, &hash, object.data(), objectLength, nullptr, 0, 0) != 0) {
        BCryptCloseAlgorithmProvider(algorithm, 0);
        return {};
    }

    HANDLE file = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr,
                              OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, nullptr);
    if (file == INVALID_HANDLE_VALUE) {
        BCryptDestroyHash(hash);
        BCryptCloseAlgorithmProvider(algorithm, 0);
        return {};
    }

    std::vector<unsigned char> buffer(1024 * 1024);
    bool ok = true;
    for (;;) {
        DWORD read = 0;
        if (!ReadFile(file, buffer.data(), static_cast<DWORD>(buffer.size()), &read, nullptr)) {
            ok = false;
            break;
        }
        if (read == 0) break;
        if (BCryptHashData(hash, buffer.data(), read, 0) != 0) {
            ok = false;
            break;
        }
    }
    CloseHandle(file);

    if (!ok || BCryptFinishHash(hash, digest.data(), hashLength, 0) != 0) {
        BCryptDestroyHash(hash);
        BCryptCloseAlgorithmProvider(algorithm, 0);
        return {};
    }

    BCryptDestroyHash(hash);
    BCryptCloseAlgorithmProvider(algorithm, 0);

    std::ostringstream out;
    out << std::hex << std::setfill('0');
    for (unsigned char byte : digest) {
        out << std::setw(2) << static_cast<int>(byte);
    }
    return out.str();
}

static void SetStatus(const wchar_t* textValue) {
    SetWindowTextW(g_status, textValue);
    UpdateWindow(g_status);
}

static void CheckForUpdate(HWND hwnd) {
    EnableWindow(g_button, FALSE);

    const auto root = ModuleDirectory();
    const auto versionPath = root / L"version.txt";
    std::string current = ReadUtf8(versionPath);
    current.erase(std::remove_if(current.begin(), current.end(),
        [](unsigned char c) { return c == '\r' || c == '\n' || c == ' ' || c == '\t'; }),
        current.end());
    if (current.empty()) current = "0.0.0";

    wchar_t tempPath[MAX_PATH]{};
    GetTempPathW(MAX_PATH, tempPath);
    const auto manifestPath = std::filesystem::path(tempPath) / L"Navegador-update.json";

    SetStatus(L"Verificando atualizações...");
    std::wstring error;
    if (!DownloadFile(kManifestUrl, manifestPath, error)) {
        SetStatus(L"Não foi possível verificar atualizações.");
        MessageBoxW(hwnd, error.c_str(), L"Atualizador do Navegador", MB_OK | MB_ICONERROR);
        EnableWindow(g_button, TRUE);
        return;
    }

    const auto json = ReadUtf8(manifestPath);
    DeleteFileW(manifestPath.c_str());

    const auto version = JsonString(json, "version");
    const auto url = JsonString(json, "url");
    auto expectedHash = JsonString(json, "sha256");
    std::transform(expectedHash.begin(), expectedHash.end(), expectedHash.begin(),
        [](unsigned char c) { return static_cast<char>(std::tolower(c)); });

    if (version.empty() || url.empty() || expectedHash.size() != 64) {
        SetStatus(L"Manifesto de atualização inválido.");
        MessageBoxW(hwnd, L"A release mais recente não contém um manifesto válido.",
                    L"Atualizador do Navegador", MB_OK | MB_ICONERROR);
        EnableWindow(g_button, TRUE);
        return;
    }

    if (!IsNewerVersion(version, current)) {
        SetStatus(L"Navegador atualizado.");
        const auto message = L"Você já está na versão mais recente (" + Utf8ToWide(current) + L").";
        MessageBoxW(hwnd, message.c_str(), L"Atualizador do Navegador", MB_OK | MB_ICONINFORMATION);
        EnableWindow(g_button, TRUE);
        return;
    }

    const auto prompt = L"Nova versão disponível: " + Utf8ToWide(version) +
                        L"\n\nBaixar e instalar agora?\n\nA pasta Data será preservada.";
    if (MessageBoxW(hwnd, prompt.c_str(), L"Atualizador do Navegador",
                    MB_YESNO | MB_ICONQUESTION) != IDYES) {
        SetStatus(L"Atualização adiada.");
        EnableWindow(g_button, TRUE);
        return;
    }

    const auto packagePath = std::filesystem::path(tempPath) /
        (L"Navegador-" + Utf8ToWide(version) + L"-windows-x64.zip");

    SetStatus(L"Baixando atualização...");
    if (!DownloadFile(Utf8ToWide(url), packagePath, error)) {
        SetStatus(L"Falha no download.");
        MessageBoxW(hwnd, error.c_str(), L"Atualizador do Navegador", MB_OK | MB_ICONERROR);
        EnableWindow(g_button, TRUE);
        return;
    }

    SetStatus(L"Validando SHA-256...");
    const auto actualHash = Sha256(packagePath);
    if (actualHash.empty() || actualHash != expectedHash) {
        DeleteFileW(packagePath.c_str());
        SetStatus(L"Atualização rejeitada.");
        MessageBoxW(hwnd,
            L"O SHA-256 do pacote não corresponde ao manifesto. A atualização não foi instalada.",
            L"Atualizador do Navegador", MB_OK | MB_ICONERROR);
        EnableWindow(g_button, TRUE);
        return;
    }

    const auto sourceScript = root / L"Updater" / L"apply-update.ps1";
    const auto tempScript = std::filesystem::path(tempPath) / L"Navegador-apply-update.ps1";
    if (!CopyFileW(sourceScript.c_str(), tempScript.c_str(), FALSE)) {
        SetStatus(L"Falha ao preparar atualização.");
        MessageBoxW(hwnd, L"Não foi possível preparar o aplicador da atualização.",
                    L"Atualizador do Navegador", MB_OK | MB_ICONERROR);
        EnableWindow(g_button, TRUE);
        return;
    }

    std::wstring parameters =
        L"-NoProfile -ExecutionPolicy Bypass -File \"" + tempScript.wstring() +
        L"\" -Package \"" + packagePath.wstring() +
        L"\" -Root \"" + root.wstring() +
        L"\" -ExpectedVersion \"" + Utf8ToWide(version) +
        L"\" -UpdaterPid " + std::to_wstring(GetCurrentProcessId());

    SetStatus(L"Aplicando atualização...");
    HINSTANCE result = ShellExecuteW(hwnd, L"open", L"powershell.exe",
                                    parameters.c_str(), root.c_str(), SW_HIDE);
    if (reinterpret_cast<INT_PTR>(result) <= 32) {
        SetStatus(L"Falha ao iniciar o aplicador.");
        MessageBoxW(hwnd, L"Não foi possível iniciar o processo de atualização.",
                    L"Atualizador do Navegador", MB_OK | MB_ICONERROR);
        EnableWindow(g_button, TRUE);
        return;
    }

    DestroyWindow(hwnd);
}

static LRESULT CALLBACK WindowProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) {
    switch (message) {
        case WM_CREATE:
            CreateWindowW(L"STATIC", L"Atualizador do Navegador",
                          WS_CHILD | WS_VISIBLE,
                          24, 20, 330, 24, hwnd, nullptr, nullptr, nullptr);
            g_status = CreateWindowW(L"STATIC", L"Pronto para verificar.",
                                     WS_CHILD | WS_VISIBLE,
                                     24, 56, 330, 24, hwnd, nullptr, nullptr, nullptr);
            g_button = CreateWindowW(L"BUTTON", L"Verificar atualizações",
                                     WS_CHILD | WS_VISIBLE | BS_DEFPUSHBUTTON,
                                     24, 94, 200, 34, hwnd,
                                     reinterpret_cast<HMENU>(1001), nullptr, nullptr);
            return 0;

        case WM_COMMAND:
            if (LOWORD(wParam) == 1001) {
                CheckForUpdate(hwnd);
                return 0;
            }
            break;

        case WM_DESTROY:
            PostQuitMessage(0);
            return 0;
    }
    return DefWindowProcW(hwnd, message, wParam, lParam);
}

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR, int show) {
    const wchar_t klass[] = L"NavegadorUpdaterWindow";

    WNDCLASSW wc{};
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = instance;
    wc.lpszClassName = klass;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = reinterpret_cast<HBRUSH>(COLOR_WINDOW + 1);

    if (!RegisterClassW(&wc)) return 1;

    HWND hwnd = CreateWindowExW(
        0, klass, L"Atualizador do Navegador",
        WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX,
        CW_USEDEFAULT, CW_USEDEFAULT, 390, 190,
        nullptr, nullptr, instance, nullptr);

    if (!hwnd) return 2;

    ShowWindow(hwnd, show);
    UpdateWindow(hwnd);

    MSG msg{};
    while (GetMessageW(&msg, nullptr, 0, 0) > 0) {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }
    return static_cast<int>(msg.wParam);
}
