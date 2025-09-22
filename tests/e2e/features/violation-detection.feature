@violation-detection
Feature: GDPR Violation Detection
  As a compliance officer
  I want the system to automatically detect GDPR violations
  So that we can maintain compliance with data protection regulations

  Background:
    Given the DOI application is running
    And the ViolationEvaluatorService is active
    And the database contains test data

  @background-service
  Scenario: ViolationEvaluatorService runs periodically
    Given the application has been running for at least 5 seconds
    When I check the application logs
    Then I should see evidence of ViolationEvaluatorService activity
    And the service should be checking for violations every second

  @reactive-violations
  Scenario: Detect access request response deadline violation
    Given a patient has made an access request 2 months ago
    And the request has not been responded to
    When the ViolationEvaluatorService runs its checks
    Then a violation should be detected
    And a ViolationException should be logged
    And reactive consequences should be executed

  @violation-tracking
  Scenario: Violation detection creates database entities
    Given there are existing compliance entities in the database
    And a violation condition exists (overdue access request)
    When the ViolationEvaluatorService completes a check cycle
    Then new D3violated entities should be created
    And the entities should have "ReactiveGenerated" names

  @health-monitoring
  Scenario: Application remains healthy during violation detection
    Given violations are being detected by the background service
    When I check the application health endpoint
    Then the application should report as healthy
    And the API should remain responsive
    And the background service should continue running

  @exception-handling
  Scenario: ViolationEvaluatorService handles exceptions gracefully
    Given the ViolationEvaluatorService is running
    When violations are detected and exceptions are thrown
    Then the service should log the violations
    And the service should continue running
    And the application should not crash