#!/usr/bin/env node

/**
 * Simple script to start the DOI application
 * Can be run independently for development
 */

const { ApplicationManager } = require('../dist/src/ApplicationManager');

async function startApp() {
    const appManager = new ApplicationManager();
    
    try {
        console.log('🚀 Starting DOI Web Application...');
        await appManager.startApplication();
        await appManager.waitForApplication();
        
        console.log('✅ Application is running and ready!');
        console.log('🌐 Health check: http://localhost:5039/health');
        console.log('📚 API docs: http://localhost:5039/api');
        console.log('');
        console.log('Press Ctrl+C to stop the application');
        
        // Keep the process alive
        process.on('SIGINT', async () => {
            console.log('\n🛑 Stopping application...');
            await appManager.stopApplication();
            process.exit(0);
        });
        
        // Keep alive
        await new Promise(() => {});
        
    } catch (error) {
        console.error('❌ Failed to start application:', error);
        process.exit(1);
    }
}

startApp();