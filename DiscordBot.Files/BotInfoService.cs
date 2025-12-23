public sealed class BotInfoService
{
    private readonly DatabaseHelper _dbh;
    private readonly DiscordLookupService _lookup;
    public BotInfoService(DatabaseHelper aDb, DiscordLookupService aLookup)
    {
        _dbh = aDb;
        _lookup = aLookup;
    }
    public async Task<BotInfoDTO> GetChannelInfo()
    {
        string lMotdChannelID = _dbh.GetMotdChannelID() ?? string.Empty;
        string lWeightedChannelID = _dbh.GetWeightedChannelID() ?? string.Empty;
        string lTargetUserID = _dbh.GetTargetUserID() ?? string.Empty;
        string lTargetChannelID = _dbh.GetTargetChannelID() ?? string.Empty;
        
        return new BotInfoDTO()
        {
            MotdChannel = await ResolveChannelName(lMotdChannelID),
            WeightedChannel = await ResolveChannelName(lWeightedChannelID),
            TargetUser = await ResolveUserName(lTargetUserID),
            TargetChannel = await ResolveChannelName(lTargetChannelID),
            HasMotdBeenPosted = true //TODO Implement logic
        };
    }
    private async Task<string?> ResolveChannelName(string? aID)
    {
        if (string.IsNullOrWhiteSpace(aID)) 
            return null;
        if(!ulong.TryParse(aID, out var lChannelID)) 
            return null;
        return await _lookup.GetDiscordChannelAsync(lChannelID);
    }
    private async Task<string?> ResolveUserName(string? aID)
    {
        if (string.IsNullOrWhiteSpace(aID)) 
            return null;
        if(!ulong.TryParse(aID, out var lUserID)) 
            return null;
        return await _lookup.GetDiscordUserAsync(lUserID);
    }
}