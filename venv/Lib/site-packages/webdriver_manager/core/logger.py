import logging

from webdriver_manager.core.config import wdm_log_level

__logger = logging.getLogger("WDM")
__logger.addHandler(logging.NullHandler())


def log(text):
    """Emitting the log message."""
    __logger.log(wdm_log_level(), text)


def set_logger(logger):
    """
    Set the global logger.

    Parameters
    ----------
    logger : object
        Any logger-like object that provides a callable ``log(level, message)`` method.

    Returns None
    """

    if not callable(getattr(logger, "log", None)):
        raise ValueError("The logger must provide a callable log(level, message) method")

    # Bind the logger input to the global logger
    global __logger
    __logger = logger
