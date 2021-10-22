using UnityEngine;
using ProjectileBallistics.Pooling;

/*
 * Required for interaction between object and projectile
 * Usage: Attach to object, that you want to be penetrable and ricochetable
*/
namespace ProjectileBallistics
{
    using HitEffects = BallisticMaterialConfig.HitEffects;
    [AddComponentMenu("Projectile Ballistics/Ballistic Material")]
    public class BallisticMaterial : MonoBehaviour
    {
        public BallisticMaterialConfig material;
        public bool enablePenetration = true;
        public bool enableRicochet = true;

        private Transform DecalParent;

        public void CreateDecal(Vector3 Position, float Radius, Vector3 Normal, HitPointType type, HitEffects config)
        {
            if (material.GetDecal(type, config) != null)
            {
                if (config.ApplyProjectileSize)
                {
                    float Size = Radius * config.DecalScale * 2.0f; //Diameter
                    CreateDecal(Position, new Vector3(Size, Size, material.GetDecal(type, config).transform.localScale.z), Normal, type, config);
                }
                else
                {
                    CreateDecal(Position, material.GetDecal(type, config).transform.localScale * config.DecalScale, Normal, type, config);
                }
            }
        }
        public void CreateDecal(Vector3 Position, Vector3 Scale, Vector3 Normal, HitPointType type, HitEffects config)
        {
            if (material != null)
            {
                GameObject sPrefab = material.GetDecal(type, config);
                if (sPrefab != null)
                {
                    if (DecalParent == null)
                    {
                        //Used to prevent child distortions(When parent has Non-Uniform scale)
                        DecalParent = new GameObject().transform;
                        DecalParent.gameObject.name = "Decal Parent";

                        DecalParent.position = transform.position;
                        DecalParent.rotation = transform.rotation;
                        DecalParent.localScale = Vector3.one;
                        DecalParent.SetParent(transform);
                    }

                    Vector3 sPosition = Position + (Normal * GlobalSettings.Effects.DecalOffset);
                    Quaternion sRotation = Quaternion.LookRotation(Normal);

                    GameObject Decal;
                    if (material.UseDecalPooling || GlobalSettings.Pooling.ForceDecalPooling)
                    {
                        Decal = PoolingSystem.Instance.Create(sPrefab, sPosition, sRotation);
                    }
                    else
                    {
                        Decal = Instantiate(sPrefab, sPosition, sRotation);
                    }

                    Decal.transform.localScale = Scale;
                    Decal.transform.SetParent(DecalParent);
                }
            }
        }

        public void CreateParticleSystem(Vector3 Position, Vector3 Normal, HitPointType type, HitEffects config)
        {
            if (material != null)
            {
                GameObject sPrefab = material.GetParticleSystem(type, config);
                if (sPrefab != null)
                {
                    Vector3 sPosition = Position + (Normal * GlobalSettings.Effects.ParticleSystemOffset);
                    Quaternion sRotation = Quaternion.LookRotation(Normal);

                    if (material.UseVFXPooling || GlobalSettings.Pooling.ForceVFXPooling)
                    {
                        PoolingSystem.Instance.Create(sPrefab, sPosition, sRotation);
                    }
                    else
                    {
                        Instantiate(sPrefab, sPosition, sRotation);
                    }
                }
            }
        }

        public void CreateAudioEffect(Vector3 Position, HitType type, HitEffects config)
        {
            if (material != null)
            {
                AudioSource sPrefab = material.GetAudioEffect(type, config);
                if (sPrefab != null)
                {
                    if (material.UseSFXPooling || GlobalSettings.Pooling.ForceSFXPooling)
                    {
                        PoolingSystem.Instance.Create(sPrefab, Position, Quaternion.identity);
                    }
                    else
                    {
                        Instantiate(sPrefab, Position, Quaternion.identity);
                    }
                }
            }
        }

        public void Hit(HitInfo hitInfo)
        {
            Projectile projectile = hitInfo.ProjectileObject.GetComponent<Projectile>();
            //Find effects info
            HitEffects config = material.effectsDefault;
            for(int i = 0; i < material.effectsOverride.Count; i++)
            {
                if(projectile.Config == material.effectsOverride[i].Type)
                {
                    config = material.effectsOverride[i];
                    break;
                }
            }
            //Create effects
            CreateDecal(hitInfo.EntryHit.point, projectile.Config.Radius, hitInfo.EntryHit.normal, HitPointType.Entry, config);
            CreateParticleSystem(hitInfo.EntryHit.point, hitInfo.EntryHit.normal, HitPointType.Entry, config);
            CreateAudioEffect(hitInfo.EntryHit.point, hitInfo.hitType, config);
            if (hitInfo.hitType == HitType.Penetration)
            {
                CreateDecal(hitInfo.ExitHit.point, projectile.Config.Radius, hitInfo.ExitHit.normal, HitPointType.Exit, config);
                CreateParticleSystem(hitInfo.ExitHit.point, hitInfo.ExitHit.normal, HitPointType.Exit, config);
            }
        }
    }
}
