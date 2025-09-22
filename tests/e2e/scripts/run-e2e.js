const { ApplicationManager } = require('../dist/src/ApplicationManager');

async function runE2E() {
    const appManager = new ApplicationManager();
    let exitCode = 0;

    try {
        // Start the application
        await appManager.startApplication();
        
        // Wait for it to be ready
        await appManager.waitForApplication();

        console.log('🧪 Running E2E tests...');
        
        // Run Cucumber tests
        const { spawn } = require('child_process');
        
        const testProcess = spawn('npm', ['test'], {
            stdio: 'inherit',
            cwd: __dirname + '/..'
        });

        exitCode = await new Promise((resolve) => {
            testProcess.on('exit', (code) => {
                resolve(code || 0);
            });
        });

        if (exitCode === 0) {
            console.log('✅ All E2E tests passed!');
        } else {
            console.log('❌ Some E2E tests failed');
        }

    } catch (error) {
        console.error('💥 E2E test execution failed:', error);
        exitCode = 1;
    } finally {
        // Always cleanup
        console.log('🧹 Cleaning up...');
        await appManager.stopApplication();
    }

    process.exit(exitCode);
}

// Handle process termination
process.on('SIGINT', async () => {
    console.log('🛑 Received SIGINT, cleaning up...');
    const appManager = new ApplicationManager();
    await appManager.stopApplication();
    process.exit(1);
});

process.on('SIGTERM', async () => {
    console.log('🛑 Received SIGTERM, cleaning up...');
    const appManager = new ApplicationManager();
    await appManager.stopApplication();
    process.exit(1);
});

runE2E();