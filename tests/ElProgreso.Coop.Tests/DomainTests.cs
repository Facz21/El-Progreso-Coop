using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Domain.Enums;
using ElProgreso.Coop.Domain.Exceptions;
using Xunit;

namespace ElProgreso.Coop.Tests;

public class DomainTests
{
    [Fact]
    public void Associate_InitialBalance_ShouldBeZero()
    {
        var associate = new Associate("1001", "Carlos Gomez");
        Assert.Equal(0m, associate.Balance);
        Assert.Empty(associate.Transactions);
    }

    [Fact]
    public void Deposit_ValidAmount_ShouldIncreaseBalance()
    {
        var associate = new Associate("1001", "Carlos Gomez");
        var tx = associate.CreateDeposit(500_000m);

        Assert.Equal(500_000m, associate.Balance);
        Assert.Single(associate.Transactions);
        Assert.Equal(TransactionType.Deposit, tx.Type);
        Assert.Equal(500_000m, tx.Amount);
        Assert.Equal(0m, tx.Commission);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Deposit_InvalidAmount_ShouldThrowInvalidTransactionAmountException(decimal amount)
    {
        var associate = new Associate("1001", "Carlos Gomez");
        Assert.Throws<InvalidTransactionAmountException>(() => associate.CreateDeposit(amount));
    }

    [Fact]
    public void Withdrawal_WithoutCommission_WhenAmountLessThanOrEqual1Million()
    {
        var associate = new Associate("1001", "Carlos Gomez");
        associate.CreateDeposit(1_500_000m);

        var tx = associate.CreateWithdrawal(1_000_000m);

        Assert.Equal(0m, tx.Commission);
        Assert.Equal(500_000m, associate.Balance);
    }

    [Fact]
    public void Withdrawal_WithCommission_WhenAmountGreaterThan1Million()
    {
        var associate = new Associate("1001", "Carlos Gomez");
        associate.CreateDeposit(2_000_000m);

        var tx = associate.CreateWithdrawal(1_000_001m);

        Assert.Equal(8_000m, tx.Commission);
        // Balance = 2,000,000 - (1,000,001 + 8,000) = 991,999
        Assert.Equal(991_999m, associate.Balance);
    }

    [Fact]
    public void Withdrawal_InsufficientFunds_ShouldThrowInsufficientFundsException()
    {
        var associate = new Associate("1001", "Carlos Gomez");
        associate.CreateDeposit(1_000_000m);

        // Attempting to withdraw 1,000,000 requires 1,000,000 + 0 commission = 1,000,000 -> OK
        // Attempting to withdraw 1,000,001 requires 1,000,001 + 8,000 = 1,008,001 > 1,000,000 -> Fails
        Assert.Throws<InsufficientFundsException>(() => associate.CreateWithdrawal(1_000_001m));
    }

    [Fact]
    public void Withdrawal_ExactlyDepletingBalanceWithCommission_ShouldSucceed()
    {
        var associate = new Associate("1001", "Carlos Gomez");
        associate.CreateDeposit(1_508_000m);

        associate.CreateWithdrawal(1_500_000m); // 1.5M + 8k fee = 1,508,000

        Assert.Equal(0m, associate.Balance);
    }
}
