using UnityEngine;
using UnityEngine.Rendering;
using ProjectileBallistics.Pooling;
using RootMotion.Dynamics;

/*
 * Script that controlls your projectile
 * Usage: Attach to object, that you want to be a projectile
*/
namespace ProjectileBallistics
{
    using pDebug = ProjectileDebug;

    public enum HitPointType
    {
        Entry,
        Exit
    }
    public enum HitType
    {
        Hit,
        Penetration,
        Ricochet
    }

    public struct HitInfo
    {
        public GameObject HitCollider;
        public GameObject HitRigidbody;
        public GameObject ProjectileObject;
        public HitType hitType;
        public float EntryEnergy, ExitEnergy;
        public RaycastHit EntryHit, ExitHit;

        public HitInfo(GameObject _HitCollider, GameObject _HitRigidbody, GameObject _ProjectileObject, HitType _hitType, float _EntryEnergy, float _ExitEnergy, RaycastHit _EntryHit, RaycastHit _ExitHit)
        {
            HitCollider = _HitCollider;
            HitRigidbody = _HitRigidbody;
            ProjectileObject = _ProjectileObject;
            hitType = _hitType;
            EntryEnergy = _EntryEnergy;
            ExitEnergy = _ExitEnergy;
            EntryHit = _EntryHit;
            ExitHit = _ExitHit;
        }
    }

    public interface IUpdateableProjectile
    {
        void UpdateProjectile(float DeltaTime);
    }
    [AddComponentMenu("Projectile Ballistics/Projectile")]
    public class Projectile : MonoBehaviour, IUpdateableProjectile, IPoolable
    {
        public ProjectileConfig Config;
        //weapon Damage below
        public int weaponDamage = 10;
        public int headshotDamage = 100;
        public PuppetMaster puppetMasta;
        private GameObject enemyHead;
        public bool Pooled { get; set; }

        protected Vector3 OldPosition;
        protected Vector3 Force;
        protected Vector3 Acceleration;
        protected Vector3 Velocity;
        //Cached variables
        protected BallisticSettings ballisticSettings;


        private Transform _transform;

        //Blood hit
        public GameObject BloodAttach;
        public GameObject[] BloodFX;
        public Transform thisCharacter;
        public Transform nearestBone;
        public int effectIdx;

        void OnEnable()
        {
            Initialize();
        }

        void Initialize()
        {
            ballisticSettings = BallisticSettings.Instance;
            _transform = transform; //transform == GetComponent<Transform>(), is bad for performance

            OldPosition = _transform.position;
            Velocity = _transform.TransformDirection(new Vector3(0, 0, Config.StartSpeed));

            ProjectileUpdateManager.Instance.Subscribe(this);
            DestroyProjectile(Config.Lifetime);

            OnProjectileInitialized();
        }
        protected virtual void OnProjectileInitialized()
        {

        }

        public void UpdateProjectile(float DeltaTime)
        {
            float SimulationTime = 0;
            while (SimulationTime < DeltaTime)
            {
                Force = GetGravity() + GetResistance(ballisticSettings.AirDensity) + GetExternalForce();
                UpdateAcceleration();
                UpdateVelocity(DeltaTime);
                UpdatePosition(DeltaTime);

                if (Velocity.magnitude <= Config.MinimalVelocity)
                {
                    pDebug.Log(string.Format("{0} has been destroyed: speed is too low.", pDebug.Object(gameObject)), gameObject);
                    DestroyProjectile();
                    return;
                }

                UpdateRotation();

                SimulationTime += CheckCollision(DeltaTime);

                OldPosition = _transform.position;

                OnProjectileUpdated();
            }
        }
        protected virtual void OnProjectileUpdated()
        {

        }

        protected void DestroyProjectile(float Time)
        {
            Invoke("DestroyProjectile", Time);
        }
        protected void DestroyProjectile()
        {
            ProjectileUpdateManager.Instance.Unsubscribe(this);
            OnProjectileDestroyed();

            if (!Pooled)
            {
                Destroy(gameObject);
            }
            else if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
        protected virtual void OnProjectileDestroyed()
        {

        }
        
        #region Hit
        RaycastHit entryHit; //Cached variable

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DeltaTime"></param>
        /// <returns>Elapsed simulation time</returns>
        float CheckCollision(float DeltaTime)
        {
            //Return value
            float ElapsedTime = DeltaTime;
            //
            float Energy = GetEnergy(); //1 -> 0; Max energy -> No Energy

            RaycastHit entryHit;
            if (ForwardRay(OldPosition, _transform.position, out entryHit))
            {
                //Entry hit detected
                Collider HitCollider = entryHit.collider;
                GameObject HitRigidbody = entryHit.transform.gameObject;
                
                float Penetration = Config.MaxPenetration * Energy;

                ElapsedTime = DeltaTime * ((entryHit.point - OldPosition).magnitude / (_transform.position - OldPosition).magnitude);
                //Debug
                pDebug.DrawTrajectory(OldPosition, entryHit.point, Energy);
                //
                BallisticMaterial ballisticMaterial = HitCollider.GetComponent<BallisticMaterial>();
                RaycastHit exitHit;


                bool ExitHitDetected = BackRay(entryHit.point, _transform.position - OldPosition, HitCollider, out exitHit);

                //Bloodsplatter effect
                if(entryHit.transform.gameObject.tag.Equals("Enemy"))
                {
                    if (effectIdx == BloodFX.Length)
                    {
                        effectIdx = 0;
                    }

                    float angle = Mathf.Atan2(entryHit.normal.x, entryHit.normal.z) * Mathf.Rad2Deg + 180;
                    var instance = Instantiate(BloodFX[effectIdx], entryHit.point, Quaternion.Euler(0, angle + 90, 0));
                    effectIdx++;
                    Debug.Log(effectIdx);

                    var settings = instance.GetComponent<BFX_BloodSettings>();
                    //settings.FreezeDecalDisappearance = InfiniteDecal;
                    //settings.LightIntensityMultiplier = DirLight.intensity;

                    nearestBone = GetNearestBone(entryHit.transform.root, entryHit.point);

                    if (nearestBone == null)
                    {

                        var attachBloodInstance = Instantiate(BloodAttach);
                        var bloodT = attachBloodInstance.transform;
                        bloodT.position = entryHit.point;
                        bloodT.localRotation = Quaternion.identity;
                        bloodT.LookAt(entryHit.point + entryHit.normal, new Vector3());
                        bloodT.Rotate(90, 0, 0);
                        Destroy(attachBloodInstance, 10);
                        Destroy(instance, 10);
                        Destroy(entryHit.transform.gameObject);
                    }
                }
                
                if (ExitHitDetected == false)
                {
                    pDebug.LogWarning(string.Format("{0}: Can't find exit point in {1}. Depth will be considered as infinite", pDebug.Object(gameObject), pDebug.Object(HitCollider.gameObject)), gameObject);
                }

                if (ballisticMaterial != null && ballisticMaterial.material != null)
                {
                    BallisticMaterialConfig material = ballisticMaterial.material;
                    //Penetration calculation
                    float PathLength = (exitHit.point - entryHit.point).magnitude;
                    float PathDensity = PathLength * material.Density;
                    float penetrationEnergyLoss = GetPenetrationEnergyLoss(PathDensity, Penetration); //0 -> 1; No Loss -> Loss
                    float penetrationEnergySave = 1 - penetrationEnergyLoss; //1 -> 0; Max power -> No Power
                    //Ricochet calculation
                    float ricochetAngle = 180 - Vector3.Angle(Velocity.normalized, entryHit.normal);
                    float ricochetChance = GetRicochetChance(ricochetAngle, material.RicochetMultiplier);
                    
                    if (ExitHitDetected && penetrationEnergySave > 0 && ballisticMaterial.enablePenetration) //Penetration
                    {
                        HitInfo hitInfo = new HitInfo(HitCollider.gameObject, HitRigidbody, gameObject, HitType.Penetration, Energy, Energy * penetrationEnergySave, entryHit, exitHit);
                        
                        OnProjectileHit(hitInfo);
                        pDebug.Log(string.Format("{0} penetrated {1}. Energy Loss: {2}", pDebug.Object(gameObject), pDebug.Object(HitCollider.gameObject), pDebug.Percent(penetrationEnergyLoss * 100.0f)), gameObject);
                        //Interact with hitted object
                        ballisticMaterial.Hit(hitInfo);
                        InteractRigidbody(HitRigidbody, entryHit.point, (Velocity * penetrationEnergyLoss) * Config.Mass);
                        //Apply new position
                        OldPosition = exitHit.point;
                        _transform.position = exitHit.point;
                        //Rotate projectile
                        Vector3 Deviation = Random.insideUnitCircle * (Config.MaxDeviation * penetrationEnergyLoss);
                        RotateProjectile(Quaternion.Euler(_transform.eulerAngles + Deviation));
                        Velocity *= penetrationEnergySave;
                        Acceleration *= penetrationEnergySave;
                    }
                    else if (Random.Range(0.0f, 1.0f) <= ricochetChance && ballisticMaterial.enableRicochet) //Ricochet
                    {
                        float energyLoss = GetRicochetEnergyLoss(ricochetAngle);
                        float energySave = 1 - energyLoss;
                        HitInfo hitInfo = new HitInfo(HitCollider.gameObject, HitRigidbody, gameObject, HitType.Ricochet, Energy, Energy * energySave, entryHit, exitHit);
                        
                        OnProjectileHit(hitInfo);
                        pDebug.Log(string.Format("{0} ricocheted from {1}. Angle: {2} Chance: {3}. Energy Loss: {4}", pDebug.Object(gameObject), pDebug.Object(HitCollider.gameObject), pDebug.Angle(ricochetAngle), pDebug.Percent(ricochetChance * 100.0f), pDebug.Percent(energyLoss * 100.0f)), gameObject);
                        //Interact with hitted object
                        ballisticMaterial.Hit(hitInfo);
                        InteractRigidbody(HitRigidbody, entryHit.point, (Velocity * energyLoss) * Config.Mass);
                        //Rotate
                        Vector3 Deviation = Random.insideUnitCircle * (10 * energyLoss);
                        RotateProjectile(Quaternion.LookRotation(Vector3.Reflect(_transform.forward, entryHit.normal)) * Quaternion.Euler(Deviation));
                        Velocity *= energySave;
                        Acceleration *= energySave;
                        //Aply new position
                        OldPosition = entryHit.point;
                        _transform.position = entryHit.point;
                    }
                    else //Hit
                    {
                        HitInfo hitInfo = new HitInfo(HitCollider.gameObject, HitRigidbody, gameObject, HitType.Hit, Energy, 0, entryHit, exitHit);
                        
                        OnProjectileHit(hitInfo);
                        pDebug.Log(string.Format("{0} has been destroyed: projectile can't penetrate {1}", pDebug.Object(gameObject), pDebug.Object(HitCollider.gameObject)), gameObject);
                        //Interact with hitted object
                        ballisticMaterial.Hit(hitInfo);
                        InteractRigidbody(HitRigidbody, entryHit.point, Velocity * Config.Mass);

                        DestroyProjectile();
                    }
                }
                else //No Ballistic Material
                {
                    HitInfo hitInfo = new HitInfo(HitCollider.gameObject, HitRigidbody, gameObject, HitType.Hit, Energy, 0, entryHit, new RaycastHit());
                    
                    OnProjectileHit(hitInfo);
                    pDebug.Log(string.Format("{0} has been destroyed: no ballistic material attached to hitted object({1})", pDebug.Object(gameObject), pDebug.Object(HitCollider.gameObject)), gameObject);
                    //Interact with hitted object
                    InteractRigidbody(HitRigidbody, entryHit.point, Velocity * Config.Mass);

                    DestroyProjectile();
                }
            }
            else
            {
                //Hit not detected
                pDebug.DrawTrajectory(OldPosition, _transform.position, Energy);
            }

            return ElapsedTime;
        }
        bool ForwardRay(Vector3 From, Vector3 To, out RaycastHit Hit)
        {
            return Physics.Raycast(new Ray(From, To - From), out Hit, (To - From).magnitude + 0.01f, ballisticSettings.ProjectileCollisionLayerMask);
        }
        bool BackRay(Vector3 Position, Vector3 Direction, Collider Target, out RaycastHit Hit)
        {
            //Check using step method
            float Step = GlobalSettings.HitDetection.BackRayStep;
            float MaxDistance = GlobalSettings.HitDetection.BackRayMaxDistance;
            for (int i = 1; i < Mathf.CeilToInt(MaxDistance / Step); i++)
            {
                Vector3 checkPos = Position + (Direction.normalized * (Step * i));

                Ray ray = new Ray(checkPos, -Direction);
                RaycastHit[] Hits = Physics.RaycastAll(ray, Vector3.Magnitude(checkPos - Position) + 0.01f, ballisticSettings.ProjectileCollisionLayerMask);
                for (int j = 0; j < Hits.Length; j++)
                {
                    if (Hits[j].collider == Target)
                    {
                        Hit = Hits[j];
                        return true;
                    }
                }
            }
            //Check using single raycast
            float Distance = 1000f;
            Ray _ray = new Ray(Position + (Direction.normalized * Distance), -Direction);
            RaycastHit[] _Hits = Physics.RaycastAll(_ray, Distance * 1.1f, ballisticSettings.ProjectileCollisionLayerMask);
            for (int j = 0; j < _Hits.Length; j++)
            {
                if (_Hits[j].collider == Target)
                {
                    Hit = _Hits[j];
                    return true;
                }
            }
            //When hit not detected
            Hit = new RaycastHit();
            return false;
        }

        public float GetEnergy()
        {
            return Velocity.magnitude / Config.StartSpeed;
        }
        public static float GetPenetrationEnergyLoss(float PathDensity, float Penetration)
        {
            return PathDensity / Penetration;
        }
        public static float GetRicochetEnergyLoss(float RicochetAngle)
        {
            return 1 - Mathf.Clamp(Mathf.Pow(RicochetAngle / 90.0f, 5.0f), 0.05f, 1.0f);
        }
        protected float GetRicochetChance(float RicochetAngle, float ChanceMultiplayer)
        {
            //0 - 0%; 1 - 100%
            return GetRicochetChance(Config, RicochetAngle, ChanceMultiplayer);
        }
        public static float GetRicochetChance(ProjectileConfig config, float RicochetAngle, float ChanceMultiplayer)
        {
            //0 - 0%; 1 - 100%
            return Mathf.Clamp(config.RicochetChance.Evaluate(RicochetAngle / 90.0f) * ChanceMultiplayer, 0, 1);
        }

        void RotateProjectile(Quaternion Rotation)
        {
            //Save local acceleration & velocity before rotating projectile
            Vector3 localAcceleration = _transform.InverseTransformDirection(Acceleration);
            Vector3 localVelocity = _transform.InverseTransformDirection(Velocity);
            //Rotate projectile
            _transform.rotation = Rotation;
            //Apply energy loss & new projectile rotation to acceleration & velocity
            Acceleration = _transform.TransformDirection(localAcceleration);
            Velocity = _transform.TransformDirection(localVelocity);
        }
        protected void InteractRigidbody(GameObject body, Vector3 Position, Vector3 Impulse)
        {
            Rigidbody rBody = body.GetComponent<Rigidbody>();
            if (rBody != null)
            {
                rBody.AddForceAtPosition(Impulse, Position, ForceMode.Impulse);
            }
        }

        ///<summary> Can be overwritten to implement take damage in your game or any other features </summary>
        ///
        //
        //
        //HERE FOR CUSTOM HIT
        //
        //
        //
        //
        //
        protected virtual void OnProjectileHit(HitInfo hit)
        {
            if(hit.HitCollider.CompareTag("Enemy"))
             {
                Enemy enemy = hit.EntryHit.transform.GetComponentInParent<Enemy>();
                enemyHead = enemy.getHead();


                if (enemy != null)
                {
                    Debug.Log(hit.EntryHit.transform.gameObject);
                    enemy.Damage(weaponDamage);
                    if(hit.EntryHit.transform.gameObject == enemyHead)
                    {
                        enemy.Damage(headshotDamage);
                    }
                }
            }

            if(hit.HitCollider.CompareTag("Barrel"))
            {
                ExplodeBarrel barrel = hit.EntryHit.transform.GetComponent<ExplodeBarrel>();
                GameObject barrelObj = hit.EntryHit.transform.gameObject;
                barrel.AddHit();
                if (barrel.getHitCount() >= 3)
                {
                    barrelObj.SetActive(false);
                    barrel.explodedBarrel.SetActive(true);
                    Rigidbody rb = barrel.explodedBarrel.gameObject.GetComponent<Rigidbody>();
                    rb.AddRelativeForce(-transform.up * 15f, ForceMode.Impulse);
                    barrel.Explode();
                }
            }
        }

        public Transform GetNearestBone(Transform characterTransform, Vector3 hitPos)
        {
            var closestPos = 10f;
            Transform closestBone = null;
            var childs = characterTransform.GetComponentsInChildren<Transform>();

            foreach (var child in childs)
            {
                var dist = Vector3.Distance(child.position, hitPos);
                if (dist < closestPos)
                {
                    closestPos = dist;
                    closestBone = child;
                }
            }

            var distRoot = Vector3.Distance(characterTransform.position, hitPos);
            if (distRoot < closestPos)
            {
                closestPos = distRoot;
                closestBone = characterTransform;
            }
            return closestBone;
        }
        //
        //
        //
        //
        //
        //
        //
        //
        //
        //
        //
        #endregion
        #region Forces
        Vector3 GetGravity()
        {
            return Physics.gravity * Config.Mass;
        }

        Vector3 GetResistance(float matDensity)
        {
            float SquareForward = Mathf.PI * Config.Radius * Config.Radius;
            float SquareSide = Mathf.PI * Config.Radius * Config.Length;
            //Get square of local velocity with saving of sign of velocity.
            Vector3 SqrLocalVelocity = _transform.InverseTransformDirection(ballisticSettings.Wind - Velocity);
            SqrLocalVelocity.x *= (SqrLocalVelocity.x > 0) ? SqrLocalVelocity.x : -SqrLocalVelocity.x;
            SqrLocalVelocity.y *= (SqrLocalVelocity.y > 0) ? SqrLocalVelocity.y : -SqrLocalVelocity.y;
            SqrLocalVelocity.z *= (SqrLocalVelocity.z > 0) ? SqrLocalVelocity.z : -SqrLocalVelocity.z;

            float ForwardResistance = (SquareForward * Config.ForwardDrag * SqrLocalVelocity.z * matDensity) / 2;
            float SideXResistance = (SquareSide * Config.SideDrag * SqrLocalVelocity.x * matDensity) / 2;
            float SideYResistance = (SquareSide * Config.SideDrag * SqrLocalVelocity.y * matDensity) / 2;

            return _transform.TransformDirection(new Vector3(SideXResistance, SideYResistance, ForwardResistance));
        }
        protected virtual Vector3 GetExternalForce()
        {
            return Vector3.zero;
        }
        #endregion
        #region Updates
        void UpdateAcceleration()
        {
            Acceleration = Force / Config.Mass;
        }
        void UpdateVelocity(float DeltaTime)
        {
            Velocity += Acceleration * DeltaTime;
        }
        void UpdatePosition(float DeltaTime)
        {
            _transform.position += Velocity * DeltaTime;
        }
        protected virtual void UpdateRotation()
        {
            _transform.rotation = Quaternion.LookRotation(_transform.position - OldPosition);
        }
        #endregion
    }

    public static class ProjectileDebug
    {
        private static BallisticSettings ballisticSettings = BallisticSettings.Instance;

        public static void Log(string Message, GameObject Sender)
        {
            if(GlobalSettings.Debug.LogMessages)
            {
                Debug.Log(string.Format("<color=green>[Projectile Ballistics]</color> {0}", Message), Sender);
            }
        }
        public static void LogWarning(string Message, GameObject Sender)
        {
            if (GlobalSettings.Debug.WarningMessages)
            {
                Debug.LogWarning(string.Format("<color=red>[Projectile Ballistics]</color> {0}", Message), Sender);
            }
        }

        private const bool TrajectoryDepthTest = false;
        public static void DrawTrajectory(Vector3 From, Vector3 To, float Energy)
        {
            if (ballisticSettings.DebugPath)
            {
                Debug.DrawLine(From, To, ballisticSettings.PathGradient.Evaluate(1.0f - Energy), ballisticSettings.PathLifetime, TrajectoryDepthTest);
            }
            if(ballisticSettings.DebugPathPositions)
            {
                Debug.DrawLine(From, From + (Vector3.up * 0.25f), Color.grey, ballisticSettings.PathLifetime, TrajectoryDepthTest);
            }
        }

        public static string Object(GameObject gameObject)
        {
            return string.Format("<i>{0}</i>", gameObject.name);
        }

        public static string Float(float value)
        {
            return string.Format("<color=blue>{0:N1}</color>", value);
        }
        public static string Int(int value)
        {
            return string.Format("<color=blue>{0}</color>", value);
        }

        public static string Percent(float value)
        {
            return string.Format("<color=blue>{0:N1}%</color>", value);
        }
        public static string Angle(float value)
        {
            return string.Format("<color=blue>{0:N1}°</color>", value);
        }


    }
}