from selenium.webdriver.common.by import By
from tests_selenium.pages.base_page import BasePage, logger

class CheckoutPage(BasePage):
    """Page Object para la pantalla de Checkout (formulario de envío y pedido)."""
    
    # Locators basados en la estructura <label> Texto <input> </label>
    STREET_INPUT = (By.XPATH, "//label[contains(normalize-space(.), 'Address')]/input")
    CITY_INPUT = (By.XPATH, "//label[contains(normalize-space(.), 'City')]/input")
    STATE_INPUT = (By.XPATH, "//label[contains(normalize-space(.), 'State')]/input")
    ZIP_CODE_INPUT = (By.XPATH, "//label[contains(normalize-space(.), 'Zip code')]/input")
    COUNTRY_INPUT = (By.XPATH, "//label[contains(normalize-space(.), 'Country')]/input")
    PLACE_ORDER_BUTTON = (By.XPATH, "//button[contains(text(), 'Place order')]")

    def wait_for_checkout_loaded(self):
        """Espera a que cargue la sección del Checkout."""
        logger.info("Esperando a que cargue la pantalla de Checkout...")
        return self.is_visible(self.PLACE_ORDER_BUTTON)

    def fill_shipping_address(self, street, city, state, zip_code, country):
        """Rellena el formulario de dirección de envío completo."""
        logger.info("Completando formulario de dirección de envío...")
        self.fill(self.STREET_INPUT, street)
        self.fill(self.CITY_INPUT, city)
        self.fill(self.STATE_INPUT, state)
        self.fill(self.ZIP_CODE_INPUT, zip_code)
        self.fill(self.COUNTRY_INPUT, country)
        logger.info("Dirección de envío ingresada correctamente.")

    def place_order(self):
        """Hace clic en el botón 'Place order' para procesar el pedido y espera redirección."""
        logger.info("Colocando la orden de compra...")
        self.click(self.PLACE_ORDER_BUTTON)
        logger.info("Botón 'Place order' pulsado. Esperando confirmación y redirección...")
        # Al enviar exitosamente, Blazor navega al historial de órdenes: /user/orders
        self.wait_for_url_contains("user/orders")
        logger.info("Pedido procesado con éxito. Redirigido a la sección de órdenes.")
class BasketCheckoutInfo:
    """Clase de conveniencia para agrupar los datos de prueba de envío."""
    def __init__(self, street="123 Calle Universitaria", city="Bogota", state="Cundinamarca", zip_code="110111", country="Colombia"):
        self.street = street
        self.city = city
        self.state = state
        self.zip_code = zip_code
        self.country = country
