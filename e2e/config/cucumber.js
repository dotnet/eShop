module.exports = {
  default: {
    paths: ['e2e/features/**/*.feature'],
    require: [
      'e2e/step-definitions/**/*.ts',
      'e2e/support/**/*.ts'
    ],
    requireModule: ['ts-node/register'],
    format: [
      'summary',  // Clean summary output
      'html:e2e/reports/html/cucumber-report.html',
      'json:e2e/reports/json/cucumber-report.json',
      'junit:e2e/reports/junit/cucumber-report.xml'
      // ReportPortal formatter disabled due to authentication issues with Cucumber agent
      // Use Playwright + ReportPortal instead (npm run test:playwright:rp)
      , './e2e/config/reportPortalFormatter.cjs'
    ],
    formatOptions: {
      snippetInterface: 'async-await',
      colorsEnabled: true
    },
    publishQuiet: true,
    parallel: 2,
    retry: 1,
    retryTagFilter: '@flaky',
    tags: 'not @wip'
  },
  
  smoke: {
    paths: ['e2e/features/**/*.feature'],
    require: [
      'e2e/step-definitions/**/*.ts',
      'e2e/support/**/*.ts'
    ],
    requireModule: ['ts-node/register'],
    format: [
      'summary',  // Clean summary output
      'json:e2e/reports/json/smoke-report.json'
      // ReportPortal formatter disabled due to authentication issues with Cucumber agent
      // Use Playwright + ReportPortal instead (npm run test:playwright:rp)
      , './e2e/config/reportPortalFormatter.cjs'
    ],
    parallel: 1,
    retry: 0,
    tags: '@smoke'
  },
  
  critical: {
    paths: ['e2e/features/**/*.feature'],
    require: [
      'e2e/step-definitions/**/*.ts',
      'e2e/support/**/*.ts'
    ],
    requireModule: ['ts-node/register'],
    format: [
      'summary',  // Clean summary output
      'json:e2e/reports/json/critical-report.json',
      'junit:e2e/reports/junit/critical-report.xml'
      // ReportPortal formatter disabled due to authentication issues with Cucumber agent
      // Use Playwright + ReportPortal instead (npm run test:playwright:rp)
      , './e2e/config/reportPortalFormatter.cjs'
    ],
    parallel: 2,
    retry: 2,
    tags: '@critical'
  },
  
  ci: {
    paths: ['e2e/features/**/*.feature'],
    require: [
      'e2e/step-definitions/**/*.ts',
      'e2e/support/**/*.ts'
    ],
    requireModule: ['ts-node/register'],
    format: [
      'summary',  // Clean summary output
      'json:e2e/reports/json/cucumber-report.json',
      'junit:e2e/reports/junit/cucumber-report.xml'
      // ReportPortal formatter disabled due to authentication issues with Cucumber agent
      // Use Playwright + ReportPortal instead (npm run test:playwright:rp)
      , './e2e/config/reportPortalFormatter.cjs'
    ],
    parallel: 4,
    retry: 2,
    tags: 'not @wip and not @flaky'
  }
};
