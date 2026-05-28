import os
import sys
import tempfile

ROOT_FOLDER_NAME = ".wdm"


def get_default_project_root_cache_path():
    base_path = sys.path[0]
    if getattr(sys, "frozen", False) or base_path.endswith(".zip"):
        base_path = os.getcwd()
    return os.path.join(base_path, ROOT_FOLDER_NAME)


DEFAULT_PROJECT_ROOT_CACHE_PATH = get_default_project_root_cache_path()


def get_default_user_home_cache_path():
    home = os.path.expanduser("~")
    if not home or home in (os.path.sep, "/") or home.startswith("~"):
        home = tempfile.gettempdir()
    return os.path.join(home, ROOT_FOLDER_NAME)


DEFAULT_USER_HOME_CACHE_PATH = get_default_user_home_cache_path()
