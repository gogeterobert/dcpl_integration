using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using FluentAssertions;
using Moq;
using MediatR;
using doi.Infrastructure;
using doi.Infrastructure.Data;
using doi.Application.Common.Interfaces;
using doi.Domain.Entities;
using doi.Application.D3violated.Commands;

namespace doi.Infrastructure.IntegrationTests;

[TestFixture]
public class ReactiveEvaluatorServiceIntegrationTests
{
    private ServiceProvider _serviceProvider;
    private ApplicationDbContext _context;
    private Mock<TimeProvider> _mockTimeProvider;
    private ReactiveEvaluatorService _reactiveEvaluatorService;

    [SetUp]
    public async Task SetUp()
    {
        // Create in-memory database
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        // Mock TimeProvider
        _mockTimeProvider = new Mock<TimeProvider>();
        services.AddSingleton(_mockTimeProvider.Object);

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Mock MediatR
        var mockMediator = new Mock<IMediator>();
        services.AddSingleton(mockMediator.Object);

        // Add the service under test
        services.AddScoped<ReactiveEvaluatorService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _reactiveEvaluatorService = _serviceProvider.GetRequiredService<ReactiveEvaluatorService>();

        // Seed the database with required entities
        await SeedDatabaseAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }

    private async Task SeedDatabaseAsync()
    {
        // Create all required entities that the ReactiveEvaluatorService needs
        var patient = new Patient { Name = "TestPatient" };
        var enrolledPatient = new EnrolledPatient { Name = "TestEnrolledPatient" };
        var medCare = new MedCare { Name = "TestMedCare" };
        var thirdParties = new ThirdParties { Name = "TestThirdParties" };
        var dataProcessing = new DataProcessing { Name = "TestDataProcessing" };
        var remindersProcessing = new RemindersProcessing { Name = "TestRemindersProcessing" };
        var d1fulfilled = new D1fulfilled { Name = "TestD1fulfilled" };
        var d1violated = new D1violated { Name = "TestD1violated" };
        var d2fulfilled = new D2fulfilled { Name = "TestD2fulfilled" };
        var d2violated = new D2violated { Name = "TestD2violated" };
        var sharedPatientData = new SharedPatientData { Name = "TestSharedPatientData" };
        
        // Create RespondAccessRequest with a specific creation date
        var respondAccessRequest = new RespondAccessRequest 
        { 
            Name = "TestRespondAccessRequest"
        };
        
        var d3fulfilled = new D3fulfilled { Name = "TestD3fulfilled" };
        var d3violated = new D3violated { Name = "TestD3violated" };
        var d4fulfilled = new D4fulfilled { Name = "TestD4fulfilled" };
        var complianceChecking = new CompianceChecking { Name = "TestComplianceChecking" };
        var d4violated = new D4violated { Name = "TestD4violated" };
        var d5fulfilled = new D5fulfilled { Name = "TestD5fulfilled" };
        var sendingReminders = new SendingReminders { Name = "TestSendingReminders" };
        var d5violated = new D5violated { Name = "TestD5violated" };

        _context.Patients.Add(patient);
        _context.EnrolledPatients.Add(enrolledPatient);
        _context.MedCares.Add(medCare);
        _context.ThirdPartiess.Add(thirdParties);
        _context.DataProcessings.Add(dataProcessing);
        _context.RemindersProcessings.Add(remindersProcessing);
        _context.D1fulfilleds.Add(d1fulfilled);
        _context.D1violateds.Add(d1violated);
        _context.D2fulfilleds.Add(d2fulfilled);
        _context.D2violateds.Add(d2violated);
        _context.SharedPatientDatas.Add(sharedPatientData);
        _context.RespondAccessRequests.Add(respondAccessRequest);
        _context.D3fulfilleds.Add(d3fulfilled);
        _context.D3violateds.Add(d3violated);
        _context.D4fulfilleds.Add(d4fulfilled);
        _context.CompianceCheckings.Add(complianceChecking);
        _context.D4violateds.Add(d4violated);
        _context.D5fulfilleds.Add(d5fulfilled);
        _context.SendingReminderss.Add(sendingReminders);
        _context.D5violateds.Add(d5violated);

        await _context.SaveChangesAsync();
    }

    [Test]
    public async Task EvaluateViolationExpressions_WhenConditionIsTrue_ShouldReturnViolation()
    {
        // Arrange
        var respondAccessRequest = await _context.RespondAccessRequests.FirstAsync();
        
        // Set the creation time to more than 1 month ago
        var creationTime = DateTimeOffset.UtcNow.AddMonths(-2);
        respondAccessRequest.Created = creationTime;
        await _context.SaveChangesAsync();

        // Mock TimeProvider to return current time (which will be more than 1 month after creation)
        _mockTimeProvider.Setup(x => x.GetUtcNow())
            .Returns(DateTimeOffset.UtcNow);

        // Act
        var result = _reactiveEvaluatorService.EvaluateViolationExpressions();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);
        result[0].Message.Should().Contain("Violation detected");
        result[0].Expression.Should().Be("DateTime.Now > RespondAccessRequest.Created.AddMonths(1)");
    }

    [Test]
    public async Task EvaluateViolationExpressions_WhenConditionIsFalse_ShouldReturnNoViolations()
    {
        // Arrange
        var respondAccessRequest = await _context.RespondAccessRequests.FirstAsync();
        
        // Set the creation time to less than 1 month ago
        var creationTime = DateTimeOffset.UtcNow.AddDays(-15);
        respondAccessRequest.Created = creationTime;
        await _context.SaveChangesAsync();

        // Mock TimeProvider to return current time (which will be less than 1 month after creation)
        _mockTimeProvider.Setup(x => x.GetUtcNow())
            .Returns(DateTimeOffset.UtcNow);

        // Act
        var result = _reactiveEvaluatorService.EvaluateViolationExpressions();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task EvaluateReactiveConditionsAsync_WhenConditionIsTrue_ShouldExecuteConsequence()
    {
        // Arrange
        var respondAccessRequest = await _context.RespondAccessRequests.FirstAsync();
        
        // Set the creation time to more than 1 month ago
        var creationTime = DateTimeOffset.UtcNow.AddMonths(-2);
        respondAccessRequest.Created = creationTime;
        await _context.SaveChangesAsync();

        // Mock TimeProvider to return current time (which will be more than 1 month after creation)
        _mockTimeProvider.Setup(x => x.GetUtcNow())
            .Returns(DateTimeOffset.UtcNow);

        // Mock MediatR to capture the command being sent
        var mockMediator = Mock.Get(_serviceProvider.GetRequiredService<IMediator>());
        mockMediator.Setup(x => x.Send(It.IsAny<CreateCreated3violatedCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult("test-result"));

        // Act
        await _reactiveEvaluatorService.EvaluateReactiveConditionsAsync();

        // Assert
        mockMediator.Verify(x => x.Send(
            It.Is<CreateCreated3violatedCommand>(cmd => cmd.Name == "ReactiveGenerated"), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Test]
    public async Task EvaluateReactiveConditionsAsync_WhenConditionIsFalse_ShouldNotExecuteConsequence()
    {
        // Arrange
        var respondAccessRequest = await _context.RespondAccessRequests.FirstAsync();
        
        // Set the creation time to less than 1 month ago
        var creationTime = DateTimeOffset.UtcNow.AddDays(-15);
        respondAccessRequest.Created = creationTime;
        await _context.SaveChangesAsync();

        // Mock TimeProvider to return current time (which will be less than 1 month after creation)
        _mockTimeProvider.Setup(x => x.GetUtcNow())
            .Returns(DateTimeOffset.UtcNow);

        // Mock MediatR to capture the command being sent
        var mockMediator = Mock.Get(_serviceProvider.GetRequiredService<IMediator>());

        // Act
        await _reactiveEvaluatorService.EvaluateReactiveConditionsAsync();

        // Assert
        mockMediator.Verify(x => x.Send(
            It.IsAny<CreateCreated3violatedCommand>(), 
            It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Test]
    public async Task Integration_ReactiveBehavior_WithSpecificTimeScenario()
    {
        // Arrange
        var respondAccessRequest = await _context.RespondAccessRequests.FirstAsync();
        
        // Scenario: RespondAccessRequest was created on January 1, 2024
        var creationTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        respondAccessRequest.Created = creationTime;
        await _context.SaveChangesAsync();

        // Current time is March 1, 2024 (2 months later - condition should be true)
        var currentTime = new DateTimeOffset(2024, 3, 1, 12, 0, 0, TimeSpan.Zero);
        _mockTimeProvider.Setup(x => x.GetUtcNow()).Returns(currentTime);

        // Mock MediatR
        var mockMediator = Mock.Get(_serviceProvider.GetRequiredService<IMediator>());
        mockMediator.Setup(x => x.Send(It.IsAny<CreateCreated3violatedCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult("test-result"));

        // Act - Test violation detection
        var violations = _reactiveEvaluatorService.EvaluateViolationExpressions();

        // Assert - Should detect violation
        violations.Should().HaveCount(1);
        violations[0].Message.Should().Contain("Violation detected");

        // Act - Test reactive behavior
        await _reactiveEvaluatorService.EvaluateReactiveConditionsAsync();

        // Assert - Should execute reactive consequence
        mockMediator.Verify(x => x.Send(
            It.Is<CreateCreated3violatedCommand>(cmd => cmd.Name == "ReactiveGenerated"), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}