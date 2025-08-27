using UnityEngine;
using Zenject;

public class CardViewInstaller : MonoInstaller
{
    [Header("Card Prefabs")]
    [SerializeField] private CardView _cardViewPrefab;
    
    [Header("Pool Settings")]
    [SerializeField] private int _initialPoolSize = 10;

    public override void InstallBindings()
    {
        // Card View Factory
        Container.BindFactory<CardData, CardView, CardViewFactory>()
            .FromComponentInNewPrefab(_cardViewPrefab)
            .AsSingle();

        // Memory Pool for CardViews
        Container.BindMemoryPool<CardView, CardView.Pool>()
            .WithInitialSize(_initialPoolSize)
            .FromComponentInNewPrefab(_cardViewPrefab)
            .UnderTransformGroup("Card Pool");
    }
}