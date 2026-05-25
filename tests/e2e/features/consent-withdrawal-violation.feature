@consent-withdrawal
Feature: Consent Withdrawal Impact on Violations
  As a compliance officer
  I want to understand how consent withdrawal affects ongoing obligations
  So that we can properly handle data processing lifecycle and violation detection

  Background:
    Given the DOI application is running
    And the ViolationEvaluatorService is active
    And the database is in a clean state

  @withdrawal-timing-issue
  Scenario: Access request violation after consent withdrawal
    Given a patient "Jane Doe" has registered
    And the patient has given explicit consent for data processing
    And the patient made an access request 2 weeks ago
    When the patient withdraws consent
    And 2 months pass from the original access request date
    And the ViolationEvaluatorService runs its checks
    Then NO d3violated violation should be detected
    And the access request should be considered void due to withdrawal
    # EXPECTED TO FAIL: System likely still creates d3violated even though 
    # dataProcessing no longer exists, creating violations in an invalid context
