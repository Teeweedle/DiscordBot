using System.ComponentModel;

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
            HasMotdBeenPosted = await HasMotdBeenPosted(DateTime.UtcNow)
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
    private async Task<bool> HasMotdBeenPosted(DateTime aDateUTC)
    {
        ulong aMOTDChannelID = ulong.Parse(_dbh.GetMotdChannelID() ?? string.Empty);
        if(aMOTDChannelID == 0) return false;
        DateTime lLastMotdDate = await _lookup.GetLastMOTDDateAsync(aMOTDChannelID);  
        return DateTime.UtcNow - lLastMotdDate <= TimeSpan.FromDays(1);
    }
}