public interface IEnemyAI
{
    /// <summary>
    /// Chamado por EnemyHealth para iniciar a lógica de Stun da IA.
    /// </summary>
    void StartStun(float duration);

    /// <summary>
    /// Chamado por EnemyHealth para forçar o estado de morte na IA.
    /// </summary>
    void SetDeadState();

    /// <summary>
    /// Chamado por EnemyHealth para verificar se o inimigo pode receber dano.
    /// Isso suporta regras como a invulnerabilidade do Espantalho.
    /// </summary>
    bool IsVulnerableToDamage();
}