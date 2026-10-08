using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public class TaskStateReset : MonoBehaviour
{
    [SerializeField] private AppManager appManager;

    private sealed class SceneState
    {
        public VirtualArtifact Artifact;
        public Transform Parent;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public Vector3 WorldScale;
        public bool WasActive;
        public VirtualArtifact.OperationType Operation;
        public VirtualPlacementTarget Target;
        public Rigidbody Body;
        public bool WasKinematic;
        public bool UsedGravity;
    }

    private readonly List<SceneState> sceneStates = new();
    private readonly List<string> databaseSnapshot = new();

    private VirtualCart cart;
    private CartFollowToggle follow;
    private bool initialized;

    public async Task<bool> InitializeAsync(
        Transform artifactsRoot,
        VirtualCart virtualCart,
        CartFollowToggle cartFollow)
    {
        if (initialized)
        {
            return true;
        }

        if (appManager == null || appManager.apiService == null ||
            artifactsRoot == null || virtualCart == null ||
            cartFollow == null)
        {
            Debug.LogError("[TaskStateReset] Assegna tutti i riferimenti.", this);
            return false;
        }

        if (virtualCart.Artifacts.Count != 0)
        {
            Debug.LogError("[TaskStateReset] Il carrello iniziale deve essere vuoto.", this);
            return false;
        }

        try
        {
            await appManager.apiService.WaitForPendingArtifactUpdatesAsync();
            List<Artifact> data =
                await appManager.apiService.GetAllArtifactsAsync();

            if (data == null || data.Count == 0)
            {
                throw new InvalidOperationException(
                    "Impossibile acquisire i dati iniziali dal database.");
            }

            databaseSnapshot.Clear();

            foreach (Artifact artifact in data)
            {
                if (artifact == null)
                {
                    throw new InvalidOperationException("Record Artifact nullo.");
                }

                // Copia indipendente dagli oggetti modificati dall'app.
                databaseSnapshot.Add(JsonUtility.ToJson(artifact));
            }

            VirtualArtifact[] artifacts =
                artifactsRoot.GetComponentsInChildren<VirtualArtifact>(true);

            if (artifacts.Length == 0)
            {
                throw new InvalidOperationException(
                    "Artifacts Root non contiene VirtualArtifact.");
            }

            sceneStates.Clear();

            foreach (VirtualArtifact artifact in artifacts)
            {
                Transform t = artifact.transform;
                Rigidbody body = artifact.GetComponent<Rigidbody>();

                // Una voce per oggetto: i reperti None possono condividere -1.
                sceneStates.Add(new SceneState
                {
                    Artifact = artifact,
                    Parent = t.parent,
                    LocalPosition = t.localPosition,
                    LocalRotation = t.localRotation,
                    LocalScale = t.localScale,
                    WorldPosition = t.position,
                    WorldRotation = t.rotation,
                    WorldScale = t.lossyScale,
                    WasActive = artifact.gameObject.activeInHierarchy,
                    Operation = artifact.Operation,
                    Target = artifact.PlacementTarget,
                    Body = body,
                    WasKinematic = body != null && body.isKinematic,
                    UsedGravity = body != null && body.useGravity
                });
            }

            cart = virtualCart;
            follow = cartFollow;
            follow.CaptureInitialPose();

            // I riferimenti sono già salvati, prima del distacco.
            foreach (SceneState state in sceneStates)
            {
                if (state.WasActive)
                {
                    state.Artifact.DetachFromWarehouse();
                }
            }

            initialized = true;
            Debug.Log("[TaskStateReset] Situazione iniziale salvata.");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[TaskStateReset] Preparazione fallita: {exception.Message}", this);
            return false;
        }
    }

    public async Task<bool> RestoreInitialStateAsync()
    {
        if (!initialized)
        {
            Debug.LogError("[TaskStateReset] Situazione iniziale non salvata.", this);
            return false;
        }

        try
        {
            appManager.ClearAppStateForExperimentalReset();

            cart.ClearCart();
            follow.ResetForNextTask();

            foreach (SceneState state in sceneStates)
            {
                if (state.Artifact == null)
                {
                    throw new InvalidOperationException(
                        "Un reperto iniziale è stato distrutto.");
                }

                state.Artifact.gameObject.SetActive(false);
            }

            // Questo await ordina le scritture: prima le conferme del task,
            // poi i PUT di ripristino.
            await appManager.apiService.WaitForPendingArtifactUpdatesAsync();
            await Task.Yield();

            // Elimina un eventuale avanzamento di deposito appena programmato.
            appManager.ClearAppStateForExperimentalReset();

            var restoredData = new List<Artifact>();

            foreach (string json in databaseSnapshot)
            {
                Artifact artifact = JsonUtility.FromJson<Artifact>(json);

                bool success =
                    await appManager.apiService.UpdateArtifactCheckedAsync(artifact);

                if (!success)
                {
                    throw new InvalidOperationException(
                        $"Ripristino database fallito per il reperto {artifact.id}.");
                }

                restoredData.Add(artifact);
            }

            appManager.RestoreArtifactCacheForExperimentalReset(restoredData);

            foreach (SceneState state in sceneStates)
            {
                RestoreArtifact(state);
            }

            Physics.SyncTransforms();
            Debug.Log("[TaskStateReset] Ripristino completato.");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[TaskStateReset] Ripristino fallito: {exception.Message}", this);
            return false;
        }
    }

    private void RestoreArtifact(SceneState state)
    {
        VirtualArtifact artifact = state.Artifact;
        Transform t = artifact.transform;

        // Resetta tempi, tentativi, fase e appartenenza al carrello.
        artifact.ConfigureForTask(state.Operation, state.Target);

        if (state.WasActive)
        {
            Vector3 position = state.WorldPosition;
            Quaternion rotation = state.WorldRotation;

            if (state.Parent != null)
            {
                position = state.Parent.TransformPoint(state.LocalPosition);
                rotation = state.Parent.rotation * state.LocalRotation;
            }

            // Rimane senza parent: mantiene la correzione delle deformazioni.
            t.SetParent(null, true);
            t.SetPositionAndRotation(position, rotation);
            t.localScale = state.WorldScale;
        }
        else
        {
            t.SetParent(state.Parent, false);
            t.localPosition = state.LocalPosition;
            t.localRotation = state.LocalRotation;
            t.localScale = state.LocalScale;
        }

        if (state.Body != null)
        {
            state.Body.isKinematic = state.WasKinematic;
            state.Body.useGravity = state.UsedGravity;
            state.Body.position = t.position;
            state.Body.rotation = t.rotation;

            if (!state.Body.isKinematic)
            {
                state.Body.velocity = Vector3.zero;
                state.Body.angularVelocity = Vector3.zero;
            }
        }

        // Chi era invisibile resta invisibile, anche se era spento il parent.
        artifact.gameObject.SetActive(state.WasActive);

        if (state.WasActive)
        {
            artifact.DetachFromWarehouse();
        }
    }
}
