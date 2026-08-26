namespace GoDutchSnelStartWebApp.Application.MyPos;

public static class MyPosKnownTransactionTypes
{
    public static readonly IReadOnlyList<(string Code, string Description)> All =
    [
        ("001", "Fee"),
        ("002", "Cash Withdrawal"),
        ("003", "Outgoing Bank Transfer"),
        ("004", "Balance Transfer"),
        ("005", "E-money Redemption"),
        ("006", "Account Funding"),
        ("007", "Original Credit"),
        ("008", "POS Purchase"),
        ("009", "Online Purchase"),
        ("010", "Internal Transfer"),
        ("011", "Refund"),
        ("012", "Money Request"),
        ("013", "Payment"),
        ("014", "Direct Debit"),
        ("015", "Pre-Authorization"),
        ("016", "MOTO Payment"),
        ("017", "MOTO Refund"),
        ("018", "MOTO Pre-Authorization"),
        ("019", "ATM Deposit"),
        ("022", "NFC Payment"),
        ("023", "ATM Surcharge"),
        ("024", "Withdrawal"),
        ("026", "Utility Bills"),
        ("035", "Payment Request"),
        ("036", "Payment Return"),
        ("037", "Reverse With Hold"),
        ("038", "Reverse Release"),
        ("039", "Commission"),
        ("040", "Cash Funding"),
        ("041", "Negative Set Off"),
        ("042", "Charge Back"),
        ("501", "Payment on POS"),
    ];
}
