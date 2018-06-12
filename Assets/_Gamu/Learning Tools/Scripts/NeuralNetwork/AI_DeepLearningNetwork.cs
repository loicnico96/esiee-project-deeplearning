using UnityEngine;
using System.Collections;
using NeuralNetwork;

public class AI_DeepLearningNetwork
{
    private NeuralNetwork.NeuralNetwork Network;
    private float[,] InputLimits;
    private float[,] OutputLimits;
    public string Filename { get; private set; }
    public int Samples { get; private set; }
    public float Variance { get { return 1 / Mathf.Sqrt(this.Samples + 1); } }

    /// <summary>
    /// Creates a new neural network. 
    /// </summary>
    /// <param name="filename">Name of the file to save/load</param>
    /// <param name="inputs">Matrix of min-max input values (used to internally convert inputs between 0 and 1)</param>
    /// <param name="outputs">Number of outputs</param>
    public AI_DeepLearningNetwork(string filename, float[,] inputs, float[,] outputs)
    {
        this.Filename = filename;
        this.InputLimits = inputs;
        this.OutputLimits = outputs;
        int[] number_neurons = new int[3];
        number_neurons[0] = inputs.Length;
        number_neurons[1] = Mathf.Max(inputs.Length, outputs.Length);
        number_neurons[2] = outputs.Length;
        try { this.Network = NeuralNetwork.NeuralNetwork.load(filename); } catch { this.Network = null; }
        if ((this.Network == null) || (this.Network.N_Inputs != inputs.Length) || (this.Network.N_Outputs != outputs.Length))
        {
            Debug.Log("[AI] Creating network \"" + filename + "\"...");
            this.Network = new NeuralNetwork.NeuralNetwork(inputs.Length, number_neurons);
            this.Reset();
        }
    }

    /// <summary>
    /// Resets the neutral network to a random state. 
    /// </summary>
    public void Reset()
    {
        this.Network.randomizeAll();
        this.Samples = 0;
    }

    /// <summary>
    /// Asks the neutral network for a result.
    /// </summary>
    /// <param name="inputs">Inputs to give to the network</param>
    /// <param name="variance">Variance applied to the result (use Variance for default behavior)</param>
    /// <returns>Output of the network for the given inputs in the current network state.</returns>
    public float[] Think(float[] inputs, float variance)
    {
        float[] outputs = this.Network.Output(ConvertInputsToNetwork(inputs));
        for (int i = 0; i < outputs.Length; i++)
        {
            outputs[i] += Random.Range(-variance, +variance);
        }
        return ConvertOutputsFromNetwork(outputs);
    }

    /// <summary>
    /// Trains the neutral network with an observed experience. 
    /// </summary>
    /// <param name="inputs">Inputs to give to the network</param>
    /// <param name="expected_outputs">Outputs expected from the network</param>
    public void Learn(float[] inputs, float[] expected_outputs)
    {
        Debug.Log("[AI] Learning network \"" + this.Filename + "\"...");
        float[][] matrix_inputs = new float[1][] { ConvertInputsToNetwork(inputs) };
        float[][] matrix_outputs = new float[1][] { ConvertOutputsToNetwork(expected_outputs) };
        this.Network.LearningAlg.Learn(matrix_inputs, matrix_outputs);
        this.Samples += 1;
    }

    /// <summary>
    /// Saves the current state of the network. 
    /// </summary>
    public void Save(string directory)
    {
        this.Network.save(directory + "/" + this.Filename);
    }

    private float[] ConvertInputsFromNetwork(float[] inputs)
    {
        float[] converted_inputs = new float[inputs.Length];
        for (int i = 0; i < inputs.Length; i++)
            converted_inputs[i] = this.InputLimits[i, 0] + inputs[i] * (this.InputLimits[i, 1] - this.InputLimits[i, 0]);
        return converted_inputs;
    }

    private float[] ConvertInputsToNetwork(float[] inputs)
    {
        float[] converted_inputs = new float[inputs.Length];
        for (int i = 0; i < inputs.Length; i++)
            converted_inputs[i] = (inputs[i] - this.InputLimits[i, 0]) / (this.InputLimits[i, 1] - this.InputLimits[i, 0]);
        return converted_inputs;
    }

    private float[] ConvertOutputsFromNetwork(float[] outputs)
    {
        float[] converted_outputs = new float[outputs.Length];
        for (int i = 0; i < outputs.Length; i++)
            converted_outputs[i] = this.OutputLimits[i, 0] + outputs[i] * (this.OutputLimits[i, 1] - this.OutputLimits[i, 0]);
        return converted_outputs;
    }

    private float[] ConvertOutputsToNetwork(float[] outputs)
    {
        float[] converted_outputs = new float[outputs.Length];
        for (int i = 0; i < outputs.Length; i++)
            converted_outputs[i] = (outputs[i] - this.OutputLimits[i, 0]) / (this.OutputLimits[i, 1] - this.OutputLimits[i, 0]);
        return converted_outputs;
    }
}
