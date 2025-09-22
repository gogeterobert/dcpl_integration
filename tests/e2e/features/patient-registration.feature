@patient-registration
Feature: Patient Registration
  As a healthcare system
  I want patients to be able to register
  So that they can be enrolled and tracked in the DOI system

  Background:
    Given the DOI application is running
    And the database is in a clean state

  @patient-enrollment
  Scenario: Patient can register successfully
    Given I am a new patient
    When I register with name "John Doe"
    Then the registration should be successful
    And I should receive a patient ID
    And an enrolled patient entity should be created

  @patient-enrollment
  Scenario: Multiple patients can register
    Given I am a new patient
    When I register with name "Alice Smith"
    Then the registration should be successful
    And I should receive a patient ID
    When I register another patient with name "Bob Johnson" 
    Then the registration should be successful
    And I should receive a different patient ID
    And enrolled patient entities should exist for both patients