/**
 * Generate HTML report from Cucumber JSON results
 * Uses cucumber-html-reporter to create a nice HTML report
 */

const reporter = require('cucumber-html-reporter');
const fs = require('fs');
const path = require('path');

// Check if JSON report exists
const jsonReportPath = path.join(__dirname, '../reports/json/cucumber-report.json');

if (!fs.existsSync(jsonReportPath)) {
  console.log('⚠️  No JSON report found. Skipping HTML report generation.');
  process.exit(0);
}

// Configure report options
const options = {
  theme: 'bootstrap',
  jsonFile: jsonReportPath,
  output: path.join(__dirname, '../reports/html/cucumber-report.html'),
  reportSuiteAsScenarios: true,
  scenarioTimestamp: true,
  launchReport: false,
  metadata: {
    'Application': 'eShop',
    'Test Environment': process.env.TEST_ENV || 'local',
    'Base URL': process.env.BASE_URL || 'http://localhost:5045',
    'Browser': 'Chromium (Playwright)',
    'Platform': process.platform,
    'Headless': process.env.HEADLESS || 'true',
    'Executed': new Date().toISOString()
  },
  failedSummaryReport: true,
  brandTitle: 'eShop E2E Test Report',
  name: 'Cucumber BDD Tests',
  columnLayout: 1
};

try {
  reporter.generate(options);
  console.log('✅ HTML report generated successfully!');
  console.log(`📄 Report location: ${options.output}`);
  console.log('💡 View report: npm run test:e2e:report');
} catch (error) {
  console.error('❌ Error generating HTML report:', error.message);
  process.exit(1);
}
