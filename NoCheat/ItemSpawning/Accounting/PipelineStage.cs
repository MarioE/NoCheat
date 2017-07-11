namespace NoCheat.ItemSpawning.Accounting
{
    /// <summary>
    ///     Indicates what stage in the pipeline a transaction is in.
    /// </summary>
    public enum PipelineStage
    {
        Simplifying = 0,
        CheckingRecipes = 1,
        CheckingConversions = 2,
        Expired = 3
    }
}
