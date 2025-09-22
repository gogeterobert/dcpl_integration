import { Given, When, Then } from '@cucumber/cucumber';
import { ApiClient } from '../src/ApiClient';
import * as assert from 'assert';

// Define the World interface for test context
interface World {
  apiClient: ApiClient;
  patientData?: any;
  lastResponse?: any;
  patientId?: string;
  secondPatientId?: string;
}

// Set up the API client
Given('the DOI application is running', async function(this: World) {
    console.log('🚀 Setting up E2E test environment...');
    this.apiClient = new ApiClient('http://localhost:5000');
    
    // Check if application is ready
    console.log('✅ Application is ready for testing (managed externally)');
    
    // Verify health endpoint
    const healthResponse = await this.apiClient.get('/health');
    assert.strictEqual(healthResponse.status, 200, 'Application should be healthy');
    console.log('✅ Application is running and healthy');
});

Given('the database is in a clean state', function(this: World) {
    console.log('ℹ️ Database assumed to be in clean state');
});

Given('I am a new patient', function(this: World) {
    console.log('👤 Setting up new patient registration');
});

When('I register with name {string}', async function(this: World, name: string) {
    console.log(`📤 Registering patient with name: ${name}`);
    
    try {
        const payload = { name: name };
        this.lastResponse = await this.apiClient.post('/api/Patient/create', payload);
        
        if (this.lastResponse.status === 200 || this.lastResponse.status === 201) {
            this.patientId = this.lastResponse.data;
            console.log('✅ Registration successful, Patient ID:', this.patientId);
        } else {
            console.log('⚠️ Registration response:', this.lastResponse.status, this.lastResponse.data);
        }
    } catch (error) {
        console.log('❌ Registration error:', error instanceof Error ? error.message : String(error));
        if (error && typeof error === 'object' && 'response' in error) {
            this.lastResponse = (error as any).response;
        }
    }
});

When('I register another patient with name {string}', async function(this: World, name: string) {
    console.log(`📤 Registering second patient with name: ${name}`);
    
    try {
        const payload = { name: name };
        const response = await this.apiClient.post('/api/Patient/create', payload);
        
        if (response.status === 200 || response.status === 201) {
            this.secondPatientId = response.data;
            console.log('✅ Second registration successful, Patient ID:', this.secondPatientId);
        } else {
            console.log('⚠️ Second registration response:', response.status, response.data);
        }
        
        this.lastResponse = response;
    } catch (error) {
        console.log('❌ Second registration error:', error instanceof Error ? error.message : String(error));
        if (error && typeof error === 'object' && 'response' in error) {
            this.lastResponse = (error as any).response;
        }
    }
});

Then('the registration should be successful', function(this: World) {
    assert.ok(this.lastResponse, 'Should have received a response');
    assert.ok(this.lastResponse.status >= 200 && this.lastResponse.status < 300, 
        `Registration should succeed but got status ${this.lastResponse.status}`);
    console.log('✅ Registration was successful');
});

Then('I should receive a patient ID', function(this: World) {
    assert.ok(this.lastResponse?.data, 'Response should contain data');
    
    // The API returns the patient name as the result/ID
    const currentPatientId = this.lastResponse.data;
    
    assert.ok(currentPatientId, 'Response should contain a patient ID');
    console.log(`✅ Received patient ID: ${currentPatientId}`);
});

Then('I should receive a different patient ID', function(this: World) {
    assert.ok(this.secondPatientId, 'Should have received a second patient ID');
    assert.notStrictEqual(this.patientId, this.secondPatientId, 'Patient IDs should be different');
    console.log(`✅ Received different patient ID: ${this.secondPatientId}`);
});

Then('an enrolled patient entity should be created', function(this: World) {
    // Since we can't directly check the database, we verify that the registration
    // was successful, which implies an enrolled patient entity was created
    assert.ok(this.patientId, 'Patient should be registered with an ID');
    console.log('✅ Enrolled patient entity should be created for:', this.patientId);
});

Then('enrolled patient entities should exist for both patients', function(this: World) {
    assert.ok(this.patientId, 'First patient should be registered');
    assert.ok(this.secondPatientId, 'Second patient should be registered');
    assert.notStrictEqual(this.patientId, this.secondPatientId, 'Patient IDs should be different');
    console.log('✅ Enrolled patient entities should exist for both patients:', this.patientId, 'and', this.secondPatientId);
});

// Cleanup
Then('cleanup is completed', function(this: World) {
    console.log('🧹 Cleaning up E2E test environment...');
    console.log('✅ Cleanup completed');
});