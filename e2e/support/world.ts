import { setWorldConstructor, World, IWorldOptions } from '@cucumber/cucumber';
import { Browser, BrowserContext, Page } from '@playwright/test';

// Import RPWorld types (without extending, to avoid type conflicts)
// We'll use composition instead of inheritance for ReportPortal integration
let RPWorld: any;
try {
  const rpModule = require('@reportportal/agent-js-cucumber');
  RPWorld = rpModule.RPWorld;
} catch (error) {
  // ReportPortal not available, will work without it
  console.warn('⚠️  ReportPortal agent not available. Tests will run without ReportPortal integration.');
}

export interface EShopWorld extends World {
  browser: Browser;
  context: BrowserContext;
  page: Page;
  baseUrl: string;
  pageLoadStart: number;
  operationStart: number;
  operationEnd: number;
  correlationId: string;
  logMessage(message: string): void;
  generateCorrelationId(): string;
  // ReportPortal methods (optional)
  rpLog?(level: string, message: string): void;
  rpAttachment?(name: string, content: Buffer | string, type?: string): void;
  rpScreenshot?(name?: string): Promise<void>;
}

class CustomWorld extends World implements EShopWorld {
  browser!: Browser;
  context!: BrowserContext;
  page!: Page;
  baseUrl: string;
  pageLoadStart: number;
  operationStart: number;
  operationEnd: number;
  correlationId: string;
  private rpWorldInstance: any;

  constructor(options: IWorldOptions) {
    super(options);
    this.baseUrl = process.env.BASE_URL || 'http://localhost:5045';
    this.pageLoadStart = Date.now();
    this.operationStart = 0;
    this.operationEnd = 0;
    this.correlationId = '';
    
    // Initialize ReportPortal World if available
    if (RPWorld) {
      try {
        this.rpWorldInstance = new RPWorld(options);
      } catch (error) {
        console.warn('⚠️  Could not initialize ReportPortal World:', error);
      }
    }
  }

  logMessage(message: string): void {
    // Only log to console if VERBOSE mode is enabled
    if (process.env.VERBOSE === 'true') {
      const timestamp = new Date().toISOString();
      console.log(`[${timestamp}] ${message}`);
    }
    
    // Always send to ReportPortal if available (regardless of VERBOSE setting)
    if (this.rpWorldInstance && typeof this.rpWorldInstance.info === 'function') {
      try {
        this.rpWorldInstance.info(message);
      } catch (error) {
        // Silently fail if ReportPortal logging fails
      }
    }
  }

  /**
   * Send log to ReportPortal with specified level
   * @param level - Log level: 'trace', 'debug', 'info', 'warn', 'error', 'fatal'
   * @param message - Log message
   */
  rpLog(level: string, message: string): void {
    if (this.rpWorldInstance) {
      const logMethod = this.rpWorldInstance[level];
      if (typeof logMethod === 'function') {
        try {
          logMethod.call(this.rpWorldInstance, message);
        } catch (error) {
          console.warn(`⚠️  ReportPortal ${level} logging failed:`, error);
        }
      } else {
        // Fallback to info
        if (typeof this.rpWorldInstance.info === 'function') {
          try {
            this.rpWorldInstance.info(message);
          } catch (error) {
            // Silently fail
          }
        }
      }
    }
  }

  /**
   * Attach file or data to ReportPortal
   * @param name - Attachment name
   * @param content - File content (Buffer or string)
   * @param type - MIME type (optional)
   */
  rpAttachment(name: string, content: Buffer | string, type?: string): void {
    if (this.rpWorldInstance && typeof this.rpWorldInstance.attach === 'function') {
      try {
        this.rpWorldInstance.attach(content, type || 'text/plain');
      } catch (error) {
        console.warn('⚠️  ReportPortal attachment failed:', error);
      }
    }
  }

  /**
   * Take screenshot and send to ReportPortal
   * @param name - Screenshot name (optional)
   */
  async rpScreenshot(name?: string): Promise<void> {
    if (this.page) {
      try {
        const screenshot = await this.page.screenshot({ fullPage: true });
        const screenshotName = name || `Screenshot-${Date.now()}`;
        
        if (this.rpWorldInstance && typeof this.rpWorldInstance.attach === 'function') {
          try {
            this.rpWorldInstance.attach(screenshot, 'image/png');
            this.logMessage(`✓ Screenshot captured: ${screenshotName}`);
          } catch (error) {
            console.warn('⚠️  ReportPortal screenshot attachment failed:', error);
          }
        }
      } catch (error) {
        this.logMessage(`⚠ Failed to capture screenshot: ${error}`);
      }
    }
  }

  // Proxy ReportPortal methods if available
  info(message: string): void {
    this.logMessage(message);
  }

  debug(message: string): void {
    this.rpLog('debug', message);
  }

  warn(message: string): void {
    if (process.env.VERBOSE === 'true') {
      console.warn(message);
    }
    this.rpLog('warn', message);
  }

  error(message: string): void {
    // Always show errors
    console.error(message);
    this.rpLog('error', message);
  }

  fatal(message: string): void {
    // Always show fatal errors
    console.error(message);
    this.rpLog('fatal', message);
  }

  trace(message: string): void {
    this.rpLog('trace', message);
  }

  attributes(attrs: Array<{key: string, value: string}>): void {
    if (this.rpWorldInstance && typeof this.rpWorldInstance.attributes === 'function') {
      try {
        this.rpWorldInstance.attributes(attrs);
      } catch (error) {
        console.warn('⚠️  ReportPortal attributes failed:', error);
      }
    }
  }

  testCaseId(id: string): void {
    if (this.rpWorldInstance && typeof this.rpWorldInstance.testCaseId === 'function') {
      try {
        this.rpWorldInstance.testCaseId(id);
      } catch (error) {
        console.warn('⚠️  ReportPortal testCaseId failed:', error);
      }
    }
  }

  description(text: string): void {
    if (this.rpWorldInstance && typeof this.rpWorldInstance.description === 'function') {
      try {
        this.rpWorldInstance.description(text);
      } catch (error) {
        console.warn('⚠️  ReportPortal description failed:', error);
      }
    }
  }

  setStatus(status: string, reason?: string): void {
    if (this.rpWorldInstance && typeof this.rpWorldInstance.setStatus === 'function') {
      try {
        this.rpWorldInstance.setStatus(status, reason);
      } catch (error) {
        console.warn('⚠️  ReportPortal setStatus failed:', error);
      }
    }
  }

  generateCorrelationId(): string {
    return `test-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
  }
}

setWorldConstructor(CustomWorld);


