using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }
    public static float TickInterval { get; private set; }

    public static event Action OnTick;
    public static event Action OnPostTick;

    [SerializeField] private float tickRate = 60f;
    [field: SerializeField] public bool UseSimulationOrder { get; private set; }

    private float _tickTimer;
    private List<PhysicsBody> _physicsBodies = new List<PhysicsBody>();

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        Physics.simulationMode = SimulationMode.Script;
        TickInterval = 1f / tickRate;
    }

    private void HandlePriorityChanged()
    {
        _physicsBodies = _physicsBodies.OrderBy(body => body.CurrentProperties.SimulationPriority).ToList();
    }

    private void Update()
    {
        _tickTimer += Time.deltaTime;

        //while loop prevents drift from framerate differences
        //e.g. time.deltaTime is 0.4s, tick interval is 1.0s
        //with an if statement, it would run after 3 frames
        //0.2s is lost
        //with the while loop, it runs after 3 frames, but 0.2s is kept for the next check

        while (_tickTimer >= TickInterval)
        {
            _tickTimer -= TickInterval;
            SimulateTick();
        }
    }

    private void SimulateTick()
    {
        if (UseSimulationOrder)
        {
            foreach (var body in _physicsBodies)
            {
                if (body != null && body.IsActive)
                {
                    body.OnPhysicsUpdate();
                }
            }
        }
        else
        {
            OnTick?.Invoke();
            Physics.Simulate(TickInterval);
            OnPostTick?.Invoke();
        }
    }

    public void RegisterPhysicsBody(PhysicsBody body)
    {
        if (!_physicsBodies.Contains(body))
        {
            _physicsBodies.Add(body);
            body.OnPriorityChanged += HandlePriorityChanged;
        }
    }

    public void UnregisterPhysicsBody(PhysicsBody body)
    {
        if (_physicsBodies.Contains(body))
        {
            body.OnPriorityChanged -= HandlePriorityChanged;
            _physicsBodies.Remove(body);
        }
    }
}
