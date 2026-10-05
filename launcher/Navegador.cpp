#define UNICODE
#define _UNICODE
#include <windows.h>
#include <shellapi.h>
#include <filesystem>
#include <string>
#include <vector>

static std::wstring QuoteArg(const std::wstring& value) {
    if (value.find_first_of(L" \t\"") == std::wstring::npos) return value;
    std::wstring out = L"\"";
    unsigned backslashes = 0;
    for (wchar_t ch : value) {
        if (ch == L'\\') { ++backslashes; continue; }
        if (ch == L'"') {
            out.append(backslashes * 2 + 1, L'\\');
            out.push_back(L'"');
            backslashes = 0;
            continue;
        }
        out.append(backslashes, L'\\');
        backslashes = 0;
        out.push_back(ch);
    }
    out.append(backslashes * 2, L'\\');
    out.push_back(L'"');
    return out;
}

static std::filesystem::path ModuleDirectory() {
    std::vector<wchar_t> buffer(32768);
    DWORD size = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
    if (size == 0 || size >= buffer.size()) return {};
    return std::filesystem::path(std::wstring(buffer.data(), size)).parent_path();
}

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR, int) {
    const auto root = ModuleDirectory();
    if (root.empty()) {
        MessageBoxW(nullptr, L"Não foi possível localizar a pasta do Navegador.", L"Navegador", MB_OK | MB_ICONERROR);
        return 1;
    }

    const auto appDir = root / L"App";
    const auto chromeExe = appDir / L"chrome.exe";
    const auto dataDir = root / L"Data";

    if (!std::filesystem::exists(chromeExe)) {
        MessageBoxW(nullptr, L"App\\chrome.exe não foi encontrado. Extraia novamente o pacote do Navegador.", L"Navegador", MB_OK | MB_ICONERROR);
        return 2;
    }

    std::error_code ec;
    std::filesystem::create_directories(dataDir, ec);
    if (ec) {
        MessageBoxW(nullptr, L"Não foi possível criar a pasta Data do perfil portátil.", L"Navegador", MB_OK | MB_ICONERROR);
        return 3;
    }

    std::wstring command = QuoteArg(chromeExe.wstring());
    command += L" --user-data-dir=" + QuoteArg(dataDir.wstring());
    command += L" --force-dark-mode --no-first-run";

    int argc = 0;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (argv) {
        for (int i = 1; i < argc; ++i) {
            command += L" ";
            command += QuoteArg(argv[i]);
        }
        LocalFree(argv);
    }

    std::vector<wchar_t> mutableCommand(command.begin(), command.end());
    mutableCommand.push_back(L'\0');

    STARTUPINFOW si{};
    si.cb = sizeof(si);
    PROCESS_INFORMATION pi{};

    if (!CreateProcessW(chromeExe.c_str(), mutableCommand.data(), nullptr, nullptr, FALSE, 0,
                        nullptr, appDir.c_str(), &si, &pi)) {
        const auto error = GetLastError();
        std::wstring message = L"Não foi possível iniciar o Chromium. Erro Windows: " + std::to_wstring(error);
        MessageBoxW(nullptr, message.c_str(), L"Navegador", MB_OK | MB_ICONERROR);
        return 4;
    }

    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);
    return 0;
}
