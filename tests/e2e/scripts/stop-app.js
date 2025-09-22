#!/usr/bin/env node

/**
 * Simple script to stop the DOI application
 */

const { ApplicationManager } = require('../dist/src/ApplicationManager');

async function stopApp() {
    const appManager = new ApplicationManager();
    
    try {
        console.log('🛑 Stopping DOI Web Application...');
        await appManager.stopApplication();
        console.log('✅ Application stopped successfully');
    } catch (error) {
        console.error('❌ Error stopping application:', error);
        process.exit(1);
    }
}

stopApp();