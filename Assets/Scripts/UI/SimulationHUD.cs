using UnityEngine;

public sealed class SimulationHUD : MonoBehaviour
{
    private SimulationManager manager;

    public void Initialize(SimulationManager simulationManager)
    {
        manager = simulationManager;
    }

    private void OnGUI()
    {
        if (manager == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(16f, 16f, 330f, 460f), GUI.skin.box);
        GUILayout.Label("球球领土战争 · 首版原型");
        GUILayout.Label($"已用时间：{manager.ElapsedTime:0.0} 秒");
        GUILayout.Label($"状态：{(manager.IsFinished ? "比赛结束" : manager.IsRunning ? "进行中" : "已暂停")}");
        GUILayout.Space(8f);

        for (var i = 0; i < manager.Teams.Count; i++)
        {
            var team = manager.Teams[i];
            var oldColor = GUI.color;
            GUI.color = team.Color;
            GUILayout.Label($"{team.Name}：领土 {manager.GetTerritoryShare(i):P1} · 存活 {team.Alive} · 击杀 {team.Kills}");
            GUI.color = oldColor;
        }

        GUILayout.Space(8f);
        GUILayout.Label("基地状态（基地血量决定胜负）");
        for (var i = 0; i < manager.Bases.Count; i++)
        {
            var team = manager.Teams[i];
            var teamBase = manager.Bases[i];
            var oldColor = GUI.color;
            GUI.color = team.Color;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{team.Name}基地：{teamBase.Health:0}/{teamBase.MaxHealth:0}");
            GUI.color = oldColor;
            if (GUILayout.Button(teamBase.IsDestroyed ? "已摧毁" : "立即发球", GUILayout.Width(72f)) && !teamBase.IsDestroyed)
            {
                manager.SendBall(i);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(manager.IsRunning ? "暂停" : "继续", GUILayout.Height(30f)))
        {
            manager.ToggleRunning();
        }

        if (GUILayout.Button("重新开始", GUILayout.Height(30f)))
        {
            manager.Restart();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0.5 倍")) manager.SetSpeed(0.5f);
        if (GUILayout.Button("1 倍")) manager.SetSpeed(1f);
        if (GUILayout.Button("2 倍")) manager.SetSpeed(2f);
        if (GUILayout.Button("4 倍")) manager.SetSpeed(4f);
        GUILayout.EndHorizontal();

        GUILayout.Label($"当前速度：{manager.SimulationSpeed:0.0} 倍");
        if (manager.IsFinished)
        {
            GUILayout.Space(8f);
            if (manager.IsDraw)
            {
                GUILayout.Label("结果：平局，所有基地都被摧毁");
            }
            else
            {
                GUILayout.Label($"胜者：{manager.Teams[manager.WinnerTeamId].Name}");
            }
        }

        GUILayout.EndArea();
    }
}
