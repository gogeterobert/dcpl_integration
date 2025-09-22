import { spawn, ChildProcess } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

export class ApplicationManager {
    private appProcess: ChildProcess | null = null;
    private readonly solutionPath: string;
    private readonly webProjectPath: string;
    private readonly pidFile: string;

    constructor() {
        this.solutionPath = path.resolve(__dirname, '../../../..');
        this.webProjectPath = path.join(this.solutionPath, 'src', 'Web');
        this.pidFile = path.join(__dirname, '..', 'app.pid');
    }

    async startApplication(): Promise<void> {
        console.log('🚀 Starting DOI Web Application...');
        
        // Check if app is already running
        if (this.isAppRunning()) {
            console.log('⚠️ Application appears to be already running. Stopping existing instance...');
            await this.stopApplication();
        }

        return new Promise((resolve, reject) => {
            // Start the .NET application
            this.appProcess = spawn('dotnet', ['run'], {
                cwd: this.webProjectPath,
                stdio: ['inherit', 'pipe', 'pipe'],
                env: {
                    ...process.env,
                    ASPNETCORE_ENVIRONMENT: 'Development',
                    ASPNETCORE_URLS: 'https://localhost:7039;http://localhost:5039'
                }
            });

            if (!this.appProcess.pid) {
                reject(new Error('Failed to start application process'));
                return;
            }

            // Save PID for cleanup
            fs.writeFileSync(this.pidFile, this.appProcess.pid.toString());

            let appStarted = false;
            let startupOutput = '';

            this.appProcess.stdout?.on('data', (data) => {
                const output = data.toString();
                startupOutput += output;
                console.log(`📋 [APP]: ${output.trim()}`);

                // Look for startup completion indicators
                if (output.includes('Now listening on:') || 
                    output.includes('Application started') ||
                    output.includes('Content root path:')) {
                    if (!appStarted) {
                        appStarted = true;
                        console.log('✅ Application started successfully!');
                        // Give it a moment to fully initialize
                        setTimeout(() => resolve(), 2000);
                    }
                }
            });

            this.appProcess.stderr?.on('data', (data) => {
                const output = data.toString();
                console.error(`❌ [APP ERROR]: ${output.trim()}`);
                
                // Don't fail on warnings, only on actual errors
                if (output.toLowerCase().includes('error') && 
                    !output.toLowerCase().includes('warning')) {
                    if (!appStarted) {
                        reject(new Error(`Application startup error: ${output}`));
                    }
                }
            });

            this.appProcess.on('error', (error) => {
                console.error('❌ Failed to start application:', error);
                reject(error);
            });

            this.appProcess.on('exit', (code, signal) => {
                console.log(`🔴 Application exited with code ${code}, signal ${signal}`);
                if (!appStarted) {
                    reject(new Error(`Application exited prematurely with code ${code}`));
                }
            });

            // Timeout after 30 seconds
            setTimeout(() => {
                if (!appStarted) {
                    console.error('⏰ Application startup timeout');
                    this.stopApplication();
                    reject(new Error('Application startup timeout'));
                }
            }, 30000);
        });
    }

    async stopApplication(): Promise<void> {
        console.log('🛑 Stopping DOI Web Application...');

        if (this.appProcess) {
            return new Promise((resolve) => {
                this.appProcess!.on('exit', () => {
                    console.log('✅ Application stopped successfully');
                    this.cleanup();
                    resolve();
                });

                // Try graceful shutdown first
                this.appProcess!.kill('SIGTERM');

                // Force kill after 5 seconds
                setTimeout(() => {
                    if (this.appProcess && !this.appProcess.killed) {
                        console.log('🔨 Force killing application...');
                        this.appProcess.kill('SIGKILL');
                    }
                    this.cleanup();
                    resolve();
                }, 5000);
            });
        }

        // Try to kill by PID if process reference is lost
        if (fs.existsSync(this.pidFile)) {
            try {
                const pid = parseInt(fs.readFileSync(this.pidFile, 'utf8'));
                process.kill(pid, 'SIGTERM');
                console.log(`✅ Killed process ${pid}`);
            } catch (error) {
                console.log('ℹ️ Process was already terminated');
            }
        }

        this.cleanup();
    }

    private cleanup(): void {
        this.appProcess = null;
        if (fs.existsSync(this.pidFile)) {
            fs.unlinkSync(this.pidFile);
        }
    }

    private isAppRunning(): boolean {
        if (this.appProcess && !this.appProcess.killed) {
            return true;
        }

        if (fs.existsSync(this.pidFile)) {
            try {
                const pid = parseInt(fs.readFileSync(this.pidFile, 'utf8'));
                process.kill(pid, 0); // Check if process exists
                return true;
            } catch (error) {
                // Process doesn't exist
                fs.unlinkSync(this.pidFile);
                return false;
            }
        }

        return false;
    }

    async waitForApplication(timeoutMs: number = 30000): Promise<void> {
        const axios = require('axios');
        const startTime = Date.now();

        console.log('⏳ Waiting for application to be ready...');

        while (Date.now() - startTime < timeoutMs) {
            try {
                const response = await axios.get('http://localhost:5039/health', {
                    timeout: 2000,
                    validateStatus: () => true // Accept any status code
                });
                
                if (response.status === 200) {
                    console.log('✅ Application health check passed!');
                    return;
                }
            } catch (error) {
                // Expected during startup
            }

            await new Promise(resolve => setTimeout(resolve, 1000));
        }

        throw new Error('Application failed to become ready within timeout');
    }
}