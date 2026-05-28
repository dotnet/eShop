from selenium.webdriver.common.by import By
from tests_selenium.pages.base_page import BasePage, logger

class CartPage(BasePage):
    """Page Object para la página de carrito de compras."""
    
    # Locators
    CART_HEADER = (By.XPATH, "//h1[contains(text(), 'Shopping bag')] | //*[contains(@class, 'page-header-title') and contains(text(), 'Shopping bag')]")
    QUANTITY_INPUT = (By.CSS_SELECTOR, "input[aria-label='product quantity']")
    UPDATE_BUTTON = (By.XPATH, "//button[contains(text(), 'Update')]")
    CHECKOUT_BUTTON = (By.CSS_SELECTOR, "a[href='checkout']")
    EMPTY_CART_MESSAGE = (By.XPATH, "//*[contains(text(), 'Your shopping bag is empty')]")
    
    def wait_for_cart_loaded(self):
        """Espera a que la página del carrito esté completamente cargada."""
        logger.info("Esperando a que cargue la página del carrito...")
        return self.is_visible(self.CART_HEADER)

    def get_item_quantity(self):
        """Retorna el valor actual de cantidad en el input de la página del carrito."""
        input_el = self.find_element(self.QUANTITY_INPUT)
        quantity = input_el.get_attribute("value")
        logger.info(f"Cantidad actual del artículo en el carrito: {quantity}")
        return int(quantity)

    def update_item_quantity(self, quantity):
        """Actualiza la cantidad de un artículo en el carrito a un valor específico."""
        logger.info(f"Modificando la cantidad del artículo a: {quantity}...")
        self.fill(self.QUANTITY_INPUT, str(quantity))
        
        # Hacer clic en Update
        self.click(self.UPDATE_BUTTON)
        logger.info("Botón 'Update' pulsado. Esperando actualización...")
        
        # Esperar a que la página se actualice (Blazor vuelve a renderizar asíncronamente)
        # Si la cantidad es mayor a 0, validamos que el input tenga el nuevo valor.
        # Si es 0, esperamos a que el mensaje de carrito vacío aparezca.
        if quantity > 0:
            def wait_for_quantity(driver):
                val = driver.find_element(*self.QUANTITY_INPUT).get_attribute("value")
                return val == str(quantity)
            from selenium.webdriver.support.ui import WebDriverWait
            WebDriverWait(self.driver, self.timeout).until(wait_for_quantity)
            logger.info("Cantidad actualizada correctamente en la interfaz.")
        else:
            self.find_element(self.EMPTY_CART_MESSAGE)
            logger.info("El artículo fue removido. Carrito vacío detectado.")

    def click_checkout(self):
        """Hace clic en el botón 'Check out' para ir a la pantalla de pago."""
        logger.info("Navegando al Checkout...")
        self.click(self.CHECKOUT_BUTTON)
        self.wait_for_url_contains("/checkout")
        logger.info("Pantalla de Checkout cargada exitosamente.")

    def is_empty_message_visible(self):
        """Verifica si el mensaje 'Your shopping bag is empty' está visible en pantalla."""
        visible = self.is_visible(self.EMPTY_CART_MESSAGE)
        logger.info(f"¿Mensaje de carrito vacío visible?: {visible}")
        return visible
