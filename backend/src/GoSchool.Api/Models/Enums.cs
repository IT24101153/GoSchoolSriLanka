namespace GoSchool.Api.Models;

public enum UserRole
{
    Admin,
    Driver,
    Parent
}

public enum AccountStatus
{
    Pending,
    Active,
    Suspended,
    Rejected
}

public enum ApplicationStatus
{
    Pending,
    Accepted,
    Declined
}

public enum PaymentMethod
{
    Cash,
    Bank
}

public enum PaymentStatus
{
    Pending,
    Completed
}
