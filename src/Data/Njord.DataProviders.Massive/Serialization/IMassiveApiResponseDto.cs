namespace Njord.DataProviders.Massive.Serialization;

internal interface IMassiveApiResponseDto
{
    #region Response metadata

    string? Status { get; set; }

    string? Error { get; set; }

    string? Message { get; set; }

    string? RequestId { get; set; }

    string? NextUrl { get; set; }

    #endregion
}
