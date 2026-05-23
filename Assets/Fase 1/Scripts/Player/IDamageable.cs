/// <summary>
/// Interface "contrato" para qualquer objeto que pode receber dano.
/// Qualquer script que implementar esta interface é obrigado a ter um método TakeDamage.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Causa dano a este objeto.
    /// </summary>
    /// <param name="damageAmount">A quantidade de dano a aplicar.</param>
    void TakeDamage(float damageAmount);
}