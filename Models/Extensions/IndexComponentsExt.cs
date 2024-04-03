namespace Models.Extensions;

public static class IndexComponentsExt
{
    public static IndexComponent SetNewValues(this IndexComponent component, IndexComponent newValues)
    {
        if (component.Id == 0)
        {
            throw new InvalidOperationException("Trying to set values to an non-existing Db component");
        }
        component.CompanyName = newValues.CompanyName;
        component.ListedIndexes = newValues.ListedIndexes;
        component.Sector = newValues.Sector;
        component.SubSector = newValues.SubSector;
        component.Ticker = newValues.Ticker;
        component.SnPWeight = newValues.SnPWeight;
        component.NasdaqWeight = newValues.NasdaqWeight;
        component.DowWeight = newValues.DowWeight;
        component.LastUpdated = newValues.LastUpdated;
        return component;
    }
}