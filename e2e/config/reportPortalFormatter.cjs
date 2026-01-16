/**
 * ReportPortal Formatter for Cucumber (CommonJS wrapper)
 * 
 * This is a CommonJS wrapper to ensure compatibility with Cucumber's
 * formatter loading mechanism on Windows.
 */

// Load environment variables FIRST
require('dotenv').config();

const { createRPFormatterClass } = require('@reportportal/agent-js-cucumber');

// Create config inline to ensure fresh values
const config = {
  // Use token property (not apiKey) for Cucumber agent
  //token: process.env.RP_API_KEY || process.env.RP_TOKEN,
  apiKey: process.env.RP_API_KEY,
  endpoint: process.env.RP_ENDPOINT || 'https://reportportal.epam.com/api/v1',
  project: process.env.RP_PROJECT || 'roberto_meza_personal',
  
  launch: process.env.RP_LAUNCH || `eShop E2E Tests - ${process.env.TEST_ENV || 'local'} - ${new Date().toISOString()}`,
  description: process.env.RP_LAUNCH_DESCRIPTION || 'End-to-end tests for eShop microservices application',
  
  attributes: [
    { key: 'test-type', value: 'e2e' },
    { key: 'framework', value: 'cucumber' },
    { key: 'browser', value: 'chromium' },
    { key: 'environment', value: process.env.TEST_ENV || 'local' },
    { key: 'application', value: 'eShop' }
  ],

  mode: process.env.RP_MODE ,//|| (process.env.TEST_ENV === 'local' ? 'DEBUG' : 'DEFAULT'),
  
  rerun: process.env.RP_RERUN === 'true',
  rerunOf: process.env.RP_RERUN_OF,
  launchId: process.env.RP_LAUNCH_ID,
  
  skippedIssue: process.env.RP_SKIPPED_ISSUE !== 'false',
  scenarioBasedStatistics: process.env.RP_SCENARIO_BASED_STATS === 'true',
  
  takeScreenshot: 'onFailure',
  debug: process.env.RP_DEBUG === 'true' && process.env.VERBOSE === 'true',  // Only debug if VERBOSE is enabled
  
  launchUuidPrint: process.env.RP_LAUNCH_UUID_PRINT === 'true',
  launchUuidPrintOutput: process.env.RP_LAUNCH_UUID_PRINT_OUTPUT || 'STDOUT',
  
  // restClientConfig: {
  //   timeout: parseInt(process.env.RP_TIMEOUT || '30000'),
  //   headers: {
  //     'User-Agent': 'eShop-E2E-Tests/1.0'
  //   }
  // }
};

// Debug log (only show if VERBOSE is enabled)
if (process.env.VERBOSE === 'true') {
  console.log('\n🔧 ReportPortal Formatter Configuration:');
  console.log(`   Token: ${config.token ? config.token.substring(0, 20) + '...' : 'NOT SET'}`);
  console.log(`   Endpoint: ${config.endpoint}`);
  console.log(`   Project: ${config.project}`);
  console.log(`   Mode: ${config.mode}`);
  console.log(`   Launch: ${config.launch}`);
  console.log('\n');
}

// Create and export the ReportPortal formatter
module.exports = createRPFormatterClass(config);
