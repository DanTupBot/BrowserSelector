> NOTE: This is an unofficial, unmaintained fork of [DanTup/BrowserSelector](https://github.com/DanTup/BrowserSelector) (which is archived/unmaintained; forks such as [slater1/BrowseRouter](https://github.com/slater1/BrowseRouter) may be more up-to-date for modern Windows).

# Browser Selector [![Build](https://github.com/DanTupBot/BrowserSelector/actions/workflows/build.yml/badge.svg)](https://github.com/DanTupBot/BrowserSelector/actions/workflows/build.yml)

Small utility to launch a different browser depending on the domain of the url being launched.

> **[Read the blog post about this here](http://blog.dantup.com/2015/09/simple-windows-browser-selector/)**.

## Windows XP Build (Unofficial Fork)

This fork was created in response to a [blog comment](https://blog.dantup.com/2015/09/simple-windows-browser-selector/#comment-6960676999) about the original `0.3.0.0` release failing on 32-bit Windows XP SP3 with `"Not a valid Win32 application"`.

Compared to the upstream `0.3.0.0` release (which targeted `.NET Framework 4.5` and PE `SubsystemVersion 6.0`), this fork:
- Targets **`.NET Framework 2.0`** (`net20`) with PE **`SubsystemVersion 5.01`**, `Prefer32Bit=false`, and `HighEntropyVA=false` via [`.github/workflows/build.yml`](.github/workflows/build.yml).
- Includes an `App.config` (`BrowserSelector.exe.config`) supporting both CLR `v4.0` and `v2.0.50727` so the same binary can run under `.NET 2.0–3.5` or `.NET 4.0` on Windows XP (and `.NET 4.x` on newer Windows).
- Updates registry handling in [`RegistrySettings.cs`](DanTup.BrowserSelector/RegistrySettings.cs) for Windows XP (`Clients\StartMenuInternet` and `http`/`https`/`ftp` protocol handlers).

> **Note:** Although GitHub Actions verifies the build output PE headers and `--create` command on `windows-latest`, this fork has **not** been tested on a real Windows XP machine and is provided as-is without support.

### Downloading the Build

1. Open the **[GitHub Actions Build runs](https://github.com/DanTupBot/BrowserSelector/actions/workflows/build.yml)** page, click the latest green run on `master`, and download the **`BrowserSelector`** zip under **Artifacts** at the bottom of the run page *(requires signing in to GitHub)*.
2. Alternatively, you can download the latest `master` workflow artifact directly without signing in via **[`nightly.link`](https://nightly.link/DanTupBot/BrowserSelector/workflows/build/master/BrowserSelector.zip)**.

### Running on Windows XP (32-bit)

1. **Prerequisite:** Ensure **.NET Framework 2.0** (or `3.0` / `3.5`) or **.NET Framework 4.0** is installed on Windows XP SP3.
2. **Extract all three files** (`BrowserSelector.exe`, `BrowserSelector.exe.config`, and `BrowserSelector.ini`) into the same folder (for example `C:\BrowserSelector`). Keep `BrowserSelector.exe.config` alongside `BrowserSelector.exe` (it is required when running on `.NET 4.0`).
3. **Configure:** Edit `BrowserSelector.ini` in Notepad so browser paths match your 32-bit XP installation (`C:\Program Files\...` rather than `C:\Program Files (x86)\...`), and remove or replace the `edge` entry.
4. **Test before registering:** Open Command Prompt (`Start` -> `Run` -> `cmd`) and test launching a URL directly:
   ```bat
   C:\BrowserSelector\BrowserSelector.exe "http://example.org/"
   ```
5. **Register / Unregister:** From an Administrator account, run:
   ```bat
   C:\BrowserSelector\BrowserSelector.exe --register
   ```
   This registers `BrowserSelector` under *Set Program Access and Defaults* (`Clients\StartMenuInternet`) and associates `http`, `https`, and `ftp` links on Windows XP. To revert back to Internet Explorer (`IEXPLORE.EXE`) and remove the registration, run:
   ```bat
   C:\BrowserSelector\BrowserSelector.exe --unregister
   ```

## Setting Up (Windows 8.1 / 10+)

1. Grab the [latest upstream release](https://github.com/DanTup/BrowserSelector/releases) (or the build from this fork's [GitHub Actions](https://github.com/DanTupBot/BrowserSelector/actions/workflows/build.yml)) and extract to a folder somewhere on your PC.
2. Open the BrowserSelector.ini file and customise paths to your browsers and domain patterns (see below).
3. Run `BrowserSelector.exe --register` from this folder to register the tool in Windows as a web browser.
4. Open the "Choose a default browser" screen in Windows (you can simply search for "default browser" from the start screen).
5. Select BrowserSelector as the default browser.

So far, it has been tested on the following:

- Windows 8.1
- Windows 10 Pro

## Usage

    BrowserSelector.exe --register
        Register as web browser

    BrowserSelector.exe --unregister
        Unregister as web browser

    BrowserSelector.exe --create
        Creates a default/sample settings file

    BrowserSelector.exe "http://example.org/"
        Launch example.org

    BrowserSelector.exe [--wait] "http://example.org/"
        Launch example.org, optionally waiting for the browser to close..

    BrowserSelector.exe "http://example.org/" "http://example.com/" [...]
        Launches multiple urls

    BrowserSelector.exe "my bookmark file.url"
        Launches the URL specified in the .url file.

    BrowserSelector.exe "my bookmark file.webloc"
        Launches the URL specified in the .webloc (osx) file.

If you use the --wait flag with multiple urls/files each will open one after the other, in order. Each waits for the previous to close before opening. Using the --wait flag is tricky, though, since many (most) browsers open new urls as a new tab in an existing instance.

To open multiple urls at the same time and wait for them, try the following:

    BrowserSelector.exe "url-or-file" "url-or-file" --wait "url-or-file"

## Config

Config is a poor mans INI file:

```ini
; Default browser is first in list
; Use `{url}` to specify UWP app browser details
[browsers]
chrome = C:\Program Files (x86)\Google\Chrome\Application\chrome.exe
ff = C:\Program Files (x86)\Mozilla Firefox\firefox.exe
edge = microsoft-edge:{url}
ie = iexplore.exe
chrome_prof8 = "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" --profile-directory="Profile 8"

; Url preferences.
; Only * is treated as a special character (wildcard).
; Matches are domain-only. Protocols and paths are ignored.
; Use "*.blah.com" for subdomains, not "*blah.com" as that would also match "abcblah.com".
[urls]
microsoft.com = ie
*.microsoft.com = ie

; Use my project-based Chrome profile
myproject.live = chrome_prof8
myproject.local = chrome_prof8

; if the key is wrapped in /'s, it is treated as a regex.
/sites\.google\.com/a/myproject.live\.com/ = chrome_prof8

google.com = chrome
visualstudio.com = edge
```

### Browsers

    chrome = C:\Program Files (x86)\Google\Chrome\Application\chrome.exe
    chrome_prof8 = "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" --profile-directory="Profile 8"

- Browser exes must be exact paths to the browser executable.
- Arguments are optional. However, if you provide arguments the exe _must_ be enclosed in quotes.
- If there are no arguments, then the exe paths do not need to be quoted.

**Special cases:**

    edge = microsoft-edge:{url}

- For special browsers, you can include the `{url}` flag. This allows better control over the browser command-line arguments.
- This is required when specifying UWP app's such as Microsoft Edge.
- By default, the url is used as an argument when launching the exe. If the `{url}` flag is specified, it will not be added to the arguments. (In other words, it _won't_ be added twice..)

### Urls

There are two ways to specify an Url. You can use simple wildcards or full regular expressions.

**Simple wildcards:**

    microsoft.com = ie
    *.microsoft.com = ie

- Only `*` is treated as a special character in URL patterns, and matches any characters (equivalent to the `.*` regex syntax).
- Only the domain part (or IP address) of a URL is checked.
- There is no implied wildcard at the start or end, so you must include these if you need them, but be aware that "microsoft.\*" will not only match "microsoft.com" and "microsoft.co.uk" but also "microsoft.somethingelse.com".

**Full regular expressions:**

```regex
	/sites\.google\.com/a/myproject.live\.com/ = chrome_prof8
```

- Full regular expressions are specified by wrapping it in /'s.
- The domain _and_ path are used in the Url comparison.
- The regular expression syntax is based on the Microsoft .NET implementation.
