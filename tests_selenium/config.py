import os

# Configuración del servidor bajo prueba (eShop-unal)
# Cambiado al puerto HTTPS real de la webapp: https://localhost:7298/
BASE_URL = os.getenv("ESHOP_BASE_URL", "https://localhost:7298")

# Credenciales predeterminadas para las pruebas de autenticación (Identity)
USERNAME = os.getenv("ESHOP_USERNAME", "bob")
PASSWORD = os.getenv("ESHOP_PASSWORD", "Pass123$")

# Configuración de las esperas explícitas de Selenium (en segundos)
DEFAULT_TIMEOUT = int(os.getenv("ESHOP_TIMEOUT", "10"))

# Ruta para almacenar la evidencia de las pruebas
EVIDENCE_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "evidencias")

# Asegurar que el directorio de evidencias exista
os.makedirs(EVIDENCE_DIR, exist_ok=True)
