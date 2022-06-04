using System;
using UnityEngine;
using Unity.Barracuda;

public class BarracudaModelRunner{

    private Model runtimeModel;
    private IWorker worker;
    private string outputLayer;

    public BarracudaModelRunner(NNModel modelAsset, string outputLayer){
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, runtimeModel);
        this.outputLayer = outputLayer;
    }

    public Tensor RunModel(Tensor observation){
        worker.Execute(observation);
        Tensor output = worker.PeekOutput(outputLayer);
        observation.Dispose();
        return output;
    }

}