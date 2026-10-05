[System.Serializable]
public class TaskItem
{
    public Artifact Artifact;
    public StorageContainerView ShelfView;

    // Fase utilizzata dalla navigazione.
    public TaskOperation Operation;

    // Attività da riportare nei log dell'app.
    public ExperimentalOperation ExperimentOperation;

    public bool Completed = false;

    // Le due fasi di un trasferimento condividono questo ID.
    public string TransferId;

    public bool IsTransfer =>
        !string.IsNullOrEmpty(TransferId);

    // Collocazioni conservate prima dell'esecuzione.
    // -1 indica che la collocazione non è presente.
    public int SourceContainerId = -1;
    public int DestinationContainerId = -1;
}

public enum ExperimentalOperation
{
    None,
    Picking,
    Putaway,
    Return,
    Transfer
}