using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace WebAppExperimental266.Services
{
    public static class CrudRecordOperations
    {
        public static OperationAuthorizationRequirement Read { get; } = new() { Name = nameof(Read) };
        public static OperationAuthorizationRequirement Owner { get; } = new() { Name = nameof(Owner) };
        public static OperationAuthorizationRequirement Edit { get; } = new() { Name = nameof(Edit) };
        public static OperationAuthorizationRequirement Delete { get; } = new() { Name = nameof(Delete) };
    }
}
