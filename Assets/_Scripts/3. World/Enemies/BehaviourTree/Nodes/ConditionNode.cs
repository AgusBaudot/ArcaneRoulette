using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using world;

public class ConditionNode : ITreeNode
{
    private ITreeNode trueNode; //A true or false node is not really necessary
    private ITreeNode flaseNode;
    private Func<bool> question;

    public ConditionNode(Func<bool> question, ITreeNode trueNode, ITreeNode falseNode)
    {
        this.question = question;
        this.trueNode = trueNode;
        this.flaseNode = falseNode;
    }
    public void Execute()
    {
        if (question.Invoke())
            trueNode.Execute();
        else
            flaseNode.Execute();
    }
}
