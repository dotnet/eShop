from selenium.webdriver.common.by import By
from tests_selenium.pages.base_page import BasePage, logger
from tests_selenium.config import BASE_URL

class LoginPage(BasePage):
    """Page Object para la autenticación de usuarios."""
    
    # Locators
    SIGN_IN_LINK = (By.CSS_SELECTOR, "a[aria-label='Sign in']")
    USERNAME_INPUT = (By.CSS_SELECTOR, "input[placeholder='Username']")
    PASSWORD_INPUT = (By.CSS_SELECTOR, "input[placeholder='Password']")
    LOGIN_BUTTON = (By.CSS_SELECTOR, "button[value='login']")
    
    # Locators de sesión activa
    LOGGED_USER_HEADER = (By.CSS_SELECTOR, "h3")
    DROPDOWN_BUTTON = (By.CSS_SELECTOR, "span.dropdown-button")
    LOGOUT_BUTTON = (By.CSS_SELECTOR, "form[action='user/logout'] button")

    def click_sign_in(self):
        """Hace clic en el enlace 'Sign in' de la barra de navegación."""
        logger.info("Abriendo pantalla de inicio de sesión...")
        self.click(self.SIGN_IN_LINK)

    def login(self, username, password):
        """Llena el formulario de login y lo envía."""
        logger.info(f"Intentando iniciar sesión con el usuario: {username}")
        self.fill(self.USERNAME_INPUT, username)
        self.fill(self.PASSWORD_INPUT, password)
        self.click(self.LOGIN_BUTTON)

    def is_logged_in_as(self, expected_username):
        """Verifica si el usuario logueado coincide con el esperado."""
        if self.is_visible(self.LOGGED_USER_HEADER):
            actual_username = self.get_text(self.LOGGED_USER_HEADER)
            logger.info(f"Sesión activa detectada para el usuario: {actual_username}")
            return actual_username.lower() == expected_username.lower()
        logger.warning("No se detectó ninguna sesión activa.")
        return False

    def logout(self):
        """Cierra la sesión del usuario activo."""
        logger.info("Cerrando sesión del usuario...")
        # En Blazor, a veces es necesario abrir el dropdown haciendo clic en él
        self.click(self.DROPDOWN_BUTTON)
        self.click(self.LOGOUT_BUTTON)
        # Esperar a que el link de Sign In vuelva a estar visible
        self.find_element(self.SIGN_IN_LINK)
        logger.info("Sesión cerrada exitosamente.")
