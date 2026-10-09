using System;

[Serializable]
public class ExperimentSettings
{
    public int id;
    public string participantId;
    public string condition;
    public string instance;
    public int taskNumber;
    public string taskText;

    public string TaskKey =>
        $"{participantId}|{condition}|{instance}|{taskNumber}";

    public bool Validate(out string error)
    {
        error = "";

        if (id != 1 || string.IsNullOrWhiteSpace(participantId))
        {
            error = "Configurazione o partecipante non valido.";
            return false;
        }

        participantId = participantId.Trim();

        if (int.TryParse(participantId, out int number))
        {
            participantId = $"P{number:D2}";
        }

        if (condition != "HoloLens" && condition != "Traditional")
        {
            error = "Condition deve essere HoloLens oppure Traditional.";
            return false;
        }

        if (instance != "A" && instance != "B")
        {
            error = "Instance deve essere A oppure B.";
            return false;
        }

        if (taskNumber < 1 || taskNumber > 9)
        {
            error = "TaskNumber deve essere compreso tra 1 e 9.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(taskText))
        {
            error = "TaskText è vuoto.";
            return false;
        }

        return true;
    }
}
