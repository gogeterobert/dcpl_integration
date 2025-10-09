import axios, { AxiosResponse } from 'axios';

export class ApiClient {
    private readonly baseUrl: string;
    private lastResponse: AxiosResponse | null = null;

    constructor(baseUrl: string = 'http://localhost:5000') {
        this.baseUrl = baseUrl;
        // Configure axios defaults
        axios.defaults.timeout = 10000;
        axios.defaults.validateStatus = () => true; // Don't throw on non-2xx status
        
        // Ignore SSL certificate errors for development
        process.env["NODE_TLS_REJECT_UNAUTHORIZED"] = "0";
    }

    async get(endpoint: string): Promise<AxiosResponse> {
        console.log(`GET ${this.baseUrl}${endpoint}`);
        this.lastResponse = await axios.get(`${this.baseUrl}${endpoint}`);
        if (this.lastResponse) {
            console.log(`Response: ${this.lastResponse.status} ${this.lastResponse.statusText}`);
        }
        return this.lastResponse!;
    }

    async post(endpoint: string, data: any): Promise<AxiosResponse> {
        console.log(`POST ${this.baseUrl}${endpoint}`);
        console.log(`Data:`, JSON.stringify(data, null, 2));
        this.lastResponse = await axios.post(`${this.baseUrl}${endpoint}`, data, {
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (this.lastResponse) {
            console.log(`Response: ${this.lastResponse.status} ${this.lastResponse.statusText}`);
        }
        return this.lastResponse!;
    }

    async put(endpoint: string, data: any): Promise<AxiosResponse> {
        console.log(`PUT ${this.baseUrl}${endpoint}`);
        this.lastResponse = await axios.put(`${this.baseUrl}${endpoint}`, data, {
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (this.lastResponse) {
            console.log(`Response: ${this.lastResponse.status} ${this.lastResponse.statusText}`);
        }
        return this.lastResponse!;
    }

    async delete(endpoint: string): Promise<AxiosResponse> {
        console.log(`DELETE ${this.baseUrl}${endpoint}`);
        this.lastResponse = await axios.delete(`${this.baseUrl}${endpoint}`);
        if (this.lastResponse) {
            console.log(`Response: ${this.lastResponse.status} ${this.lastResponse.statusText}`);
        }
        return this.lastResponse!;
    }

    getLastResponse(): AxiosResponse | null {
        return this.lastResponse;
    }

    async checkHealth(): Promise<boolean> {
        try {
            const response = await this.get('/health');
            return response.status === 200;
        } catch (error) {
            return false;
        }
    }

    async waitForHealth(timeoutMs: number = 30000): Promise<void> {
        const startTime = Date.now();
        
        while (Date.now() - startTime < timeoutMs) {
            if (await this.checkHealth()) {
                console.log('Application is healthy');
                return;
            }
            await new Promise(resolve => setTimeout(resolve, 1000));
        }
        
        throw new Error('Application health check timeout');
    }
}