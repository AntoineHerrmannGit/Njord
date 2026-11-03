namespace Njord.DataProviders.Massive.Serialization;

internal interface IMassivePagedResponseDto<TItem> : IMassiveApiResponseDto
{
    #region Paginated response data

    List<TItem>? Results { get; set; }

    #endregion
}
