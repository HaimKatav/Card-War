using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Core.Pipeline;
using CardWar.Core.Mediator;
using CardWar.Core.Context;
using CardWar.Core.Commands.System;
using CardWar.Common.States;
public class TestCommandPipeline : MonoBehaviour
{
    private ICommandPipeline _pipeline;
    private IGameMediator _mediator;
    
    private async void Start()
    {
        _pipeline = new CommandPipeline();
        _mediator = new GameMediator();
        
        var context = GameContext.Create(AppState.InGame, GameState.PlayerTurn);
        
        Debug.Log("Testing PauseGameCommand...");
        var pauseCommand = new PauseGameCommand();
        var pauseResult = await _pipeline.ExecuteAsync(pauseCommand, context);
        
        if (pauseResult.IsSuccess)
        {
            Debug.Log("PauseGameCommand succeeded");
            
            Debug.Log("Testing ResumeGameCommand...");
            var resumeCommand = new ResumeGameCommand();
            var resumeContext = pauseResult.Context;
            var resumeResult = await _pipeline.ExecuteAsync(resumeCommand, resumeContext);
            
            if (resumeResult.IsSuccess)
            {
                Debug.Log("ResumeGameCommand succeeded");
                Debug.Log("ALL TESTS PASSED");
            }
        }
    }
    
    private void OnDestroy()
    {
        _pipeline?.Dispose();
        _mediator?.Dispose();
    }
}