from packaging import version

from webdriver_manager.core.driver import Driver
from webdriver_manager.core.logger import log
from webdriver_manager.core.os_manager import ChromeType

import gzip
import json
import zlib

CHROME_FOR_TESTING_LATEST_PATCH_VERSIONS_PER_BUILD_URL = (
    "https://googlechromelabs.github.io/chrome-for-testing/latest-patch-versions-per-build.json"
)
CHROME_FOR_TESTING_LATEST_VERSIONS_PER_MILESTONE_URL = (
    "https://googlechromelabs.github.io/chrome-for-testing/latest-versions-per-milestone.json"
)
CHROME_FOR_TESTING_KNOWN_GOOD_VERSIONS_URL = (
    "https://googlechromelabs.github.io/chrome-for-testing/known-good-versions-with-downloads.json"
)


class ChromeDriver(Driver):

    def __init__(
            self,
            name,
            driver_version,
            url,
            latest_release_url,
            http_client,
            os_system_manager,
            chrome_type=ChromeType.GOOGLE
    ):
        super(ChromeDriver, self).__init__(
            name,
            driver_version,
            url,
            latest_release_url,
            http_client,
            os_system_manager
        )
        self._browser_type = chrome_type

    def get_driver_download_url(self, os_type):
        driver_version_to_download = self.get_driver_version_to_download()
        # For Mac ARM CPUs after version 106.0.5249.61 the format of OS type changed
        # to more unified "mac_arm64". For newer versions, it'll be "mac_arm64"
        # by default, for lower versions we replace "mac_arm64" to old format - "mac64_m1".
        if version.parse(driver_version_to_download) < version.parse("106.0.5249.61"):
            os_type = os_type.replace("mac_arm64", "mac64_m1")

        if version.parse(driver_version_to_download) >= version.parse("115"):
            if os_type == "mac64":
                os_type = "mac-x64"
            if os_type in ["mac_64", "mac64_m1", "mac_arm64"]:
                os_type = "mac-arm64"

            modern_version_url = self.get_url_for_version_and_platform(driver_version_to_download, os_type)
            log(f"Modern chrome version {modern_version_url}")
            return modern_version_url

        return f"{self._url}/{driver_version_to_download}/{self.get_name()}_{os_type}.zip"

    def get_browser_type(self):
        return self._browser_type

    def get_latest_release_version(self):
        determined_browser_version = self.get_browser_version_from_os()
        log(f"Get LATEST {self._name} version for {self._browser_type}")

        if determined_browser_version is not None and version.parse(determined_browser_version) >= version.parse("115"):
            return self._latest_cft_version_for_browser_version(determined_browser_version)

        elif determined_browser_version is not None:
            # Remove the build version (the last segment) from determined_browser_version for version < 115
            determined_browser_version = ".".join(determined_browser_version.split(".")[:3])
            latest_release_url = f"{self._latest_release_url}_{determined_browser_version}"
        else:
            latest_release_url = self._latest_release_url

        resp = self._http_client.get(url=latest_release_url)
        return resp.text.rstrip()

    def _latest_cft_version_for_browser_version(self, browser_version):
        browser_build_version = ".".join(browser_version.split(".")[:3])
        browser_milestone = browser_version.split(".")[0]

        latest_patch_versions = self._get_cft_json(
            CHROME_FOR_TESTING_LATEST_PATCH_VERSIONS_PER_BUILD_URL
        )
        builds = latest_patch_versions.get("builds") or {}
        build = builds.get(browser_build_version)

        if build is not None:
            chromedriver_version = build.get("version")
            if chromedriver_version:
                return chromedriver_version

            self._raise_missing_chromedriver_version(
                browser_version=browser_version,
                checked_urls=[CHROME_FOR_TESTING_LATEST_PATCH_VERSIONS_PER_BUILD_URL],
                details=(
                    f"Chrome for Testing metadata contains an entry for build "
                    f"{browser_build_version}, but it does not contain a driver version."
                ),
            )

        latest_milestone_versions = self._get_cft_json(
            CHROME_FOR_TESTING_LATEST_VERSIONS_PER_MILESTONE_URL
        )
        milestones = latest_milestone_versions.get("milestones") or {}
        milestone = milestones.get(browser_milestone)

        if milestone is not None:
            chromedriver_version = milestone.get("version")
            if chromedriver_version:
                return chromedriver_version

            self._raise_missing_chromedriver_version(
                browser_version=browser_version,
                checked_urls=[
                    CHROME_FOR_TESTING_LATEST_PATCH_VERSIONS_PER_BUILD_URL,
                    CHROME_FOR_TESTING_LATEST_VERSIONS_PER_MILESTONE_URL,
                ],
                details=(
                    f"Chrome for Testing metadata contains an entry for milestone "
                    f"{browser_milestone}, but it does not contain a driver version."
                ),
            )

        self._raise_missing_chromedriver_version(
            browser_version=browser_version,
            checked_urls=[
                CHROME_FOR_TESTING_LATEST_PATCH_VERSIONS_PER_BUILD_URL,
                CHROME_FOR_TESTING_LATEST_VERSIONS_PER_MILESTONE_URL,
            ],
            details=(
                f"No entry for build {browser_build_version} and no entry for "
                f"milestone {browser_milestone} were found."
            ),
        )

    def _get_cft_json(self, url):
        response = self._http_client.get(url)

        try:
            return self._parse_json_response(response)
        except ValueError as error:
            raise ValueError(
                f"Could not parse Chrome for Testing metadata from {url}."
            ) from error

    def _raise_missing_chromedriver_version(self, browser_version, checked_urls, details):
        raise ValueError(
            f"Could not find a compatible {self._name} version for "
            f"{self._browser_type} {browser_version}. "
            f"Checked Chrome for Testing endpoints: {', '.join(checked_urls)}. "
            f"Possible reasons: your browser was updated before the matching driver "
            f"was published, the browser version is too new, or this version is no "
            f"longer available in Chrome for Testing. "
            f"Try pinning browser/driver versions or installing a supported "
            f"Chrome/Chromium version. Details: {details}"
        )

    def get_url_for_version_and_platform(self, browser_version, platform):
        url = "https://googlechromelabs.github.io/chrome-for-testing/known-good-versions-with-downloads.json"
        response = self._http_client.get(url)
        try:
            data = self._parse_json_response(response)
        except ValueError as error:
            raise ValueError(
                f"Could not parse Chrome for Testing metadata from {url}."
            ) from error
        versions = data["versions"]

        if version.parse(browser_version) >= version.parse("115"):
            short_version = ".".join(browser_version.split(".")[:3])
            compatible_versions = [v for v in versions if short_version in v["version"]]
            if compatible_versions:
                latest_version = compatible_versions[-1]
                log(f"WebDriver version {latest_version['version']} selected")
                downloads = latest_version["downloads"]["chromedriver"]
                for d in downloads:
                    if d["platform"] == platform:
                        return d["url"]
                if platform == "win64":
                    for d in downloads:
                        if d["platform"] == "win32":
                            return d["url"]
        else:
            for v in versions:
                if v["version"] == browser_version:
                    downloads = v["downloads"]["chromedriver"]
                    for d in downloads:
                        if d["platform"] == platform:
                            return d["url"]

        raise Exception(f"No such driver version {browser_version} for {platform}")

    def _parse_json_response(self, response):
        try:
            return response.json()
        except Exception:
            pass

        text = getattr(response, "text", None)
        if text:
            try:
                return json.loads(text)
            except ValueError:
                pass

        raw = getattr(response, "content", None)
        if not raw:
            raise ValueError("Response body is empty")

        headers = getattr(response, "headers", {}) or {}
        encoding = (headers.get("Content-Encoding") or "").lower().strip()

        if encoding == "gzip":
            raw = gzip.decompress(raw)
        elif encoding == "br":
            try:
                import brotli
            except ImportError as error:
                raise ValueError(
                    "Response is brotli-compressed but brotli package is not installed"
                ) from error
            raw = brotli.decompress(raw)
        elif encoding == "deflate":
            raw = zlib.decompress(raw)

        return json.loads(raw.decode("utf-8"))
