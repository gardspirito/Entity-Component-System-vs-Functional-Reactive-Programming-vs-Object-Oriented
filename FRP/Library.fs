namespace FrpImplementation

open UnityEngine
open FSharpPlus
open FSharpPlus.Data

module FRP =
  // F#, just like C#, does not support higher-kinded polymorphism. This makes use of MSF more complicated.
  // For this demostration we use three different types to represent MSF over three different monad transformers
  type MSF<'a, 'b> = MSF of ('a -> 'b * MSF<'a, 'b>) // MSF over Identity monad
  type MSFR<'r, 'a, 'b> = MSFR of ('a -> Reader<'r, 'b * MSFR<'r, 'a, 'b>>) // MSF over ReaderT Identity monad
  type MSFRL<'r, 'a, 'b> = MSFRL of ('a -> ListT<Reader<'r, list<'b * MSFRL<'r, 'a, 'b>>>>) // MSF over ListT (ReaderT r Identity) monad
  // Note that ListT monad used here is not considered "safe" (as it is not a proper monad transformer)

  // runReaderS allows to embed MSF over ReaderT Identity into MSF over Identity
  // i. e. runReaderS allows to enrich some computation with immutable environmental variable, available at any point of execution
  let rec runReaderS (MSFR f : MSFR<'r, 'a, 'b>) : MSF<'r * 'a, 'b>
    = MSF (fun (r, a) ->
        let (res, next) = Reader.run (f a) r
        (res, runReaderS next))

  // runListS allows to embed MSF over ListT (ReaderT r Identity) into MSF over ReaderT Identity
  // i. e. runListS allows to broadcast some input to numerous independent computation and collect results
  // each such computation is able to fork or "die".
  let runListS (msf1 : MSFRL<'r, 'a, 'b>) : MSFR<'r, 'a, list<'b>>
    = let rec runListS' msfs = MSFR (fun a -> Reader (fun env ->
        let (results, next) = 
          [ for (MSFRL msf) in msfs -> (msf a |> ListT.run |> Reader.run) env ]
          |> List.collect id
          |> List.unzip
        (results, runListS' next)))
      runListS' [msf1]

  // fromList allows to create a single MSF over (ListT (ReaderT Identity)) from a list of such computations.
  // Internally, fromList is implemented as a single computation that forks into N independent computations.
  let fromList (msfs : list<MSFRL<'r, 'a, 'b>>) : MSFRL<'r, 'a, 'b>
    = MSFRL (fun a -> ListT (Reader (fun env ->
          [ for (MSFRL msf) in msfs -> (msf a |> ListT.run |> Reader.run) env ]
          |> List.collect id
      )))

  // Mealy state machine
  // Allows to express an MSF computation that accepts some input, and, based on input and old state,
  // produces output and new state.
  // Internally, mealy uses statefulness of MSF to preserve state (step of mealy computation produces a new mealy computation with new state)
  let rec mealy (f : 'a -> 's -> 'b * 's) (s : 's) : MSFRL<'r, 'a, 'b>
    = MSFRL (fun a -> monad {
        let (b, newS) = f a s
        return (b, mealy f newS)
      })

  // Feedback loop
  // feedbackI extends an existing computation with state.
  let rec feedbackI (s : 's) (MSF f : MSF<'a * 's, 'b * 's>) : MSF<'a, 'b>
    = MSF (fun a ->
        let ((b, newS), next) = f (a, s)
        (b, feedbackI newS next))
  
  // Accumulator
  // Accepts some input, an, based on input and old state, produces and outputs new state.
  let rec accumulateWith (f : 'a -> 's -> 's) (s : 's) : MSFRL<'r, 'a, 's>
    = mealy (fun a -> f a >> fun x -> (x, x)) s

  // arrI allows to lift any pure function into a MSF.
  let rec arrI (f : 'a -> 'b) : MSF<'a, 'b>
    = MSF (fun a -> (f a, arrI f))

  // arrM is similar to arrI, but accepts the function access the monadic context.
  let rec arrM (f : 'a -> ListT<Reader<'r, list<'b>>>) : MSFRL<'r, 'a, 'b>
    = MSFRL (fun a -> monad {
        let! b = f a
        return (b, arrM f)
      })

  let arr (f : 'a -> 'b) : MSFRL<'r, 'a, 'b>
    = arrM (fun a -> monad { return f a })

  // Duplication
  // dup is a simple MSF that accepts some input and returns two copies of it
  let dup unit : MSFRL<'r, 'a, 'a * 'a>
    = arr (fun x -> (x, x))

  // Allows to execute two MSFs in a "parallel" fashion over independent inputs:
  // |.| combines two independent MSFs with different inputs into a single computation that
  // accepts both inputs, processes them and returns both outputs.
  // Note that this implementation of |.| does not actually make use of multiple threads.
  let rec (|.|) ((MSFRL f1) : MSFRL<'r, 'a1, 'b1>) ((MSFRL f2) : MSFRL<'r, 'a2, 'b2>)
    : MSFRL<'r, 'a1 * 'a2, 'b1 * 'b2>
    = MSFRL (fun (a1, a2) -> monad {
        let! (b1, next1) = f1 a1
        let! (b2, next2) = f2 a2
        return ((b1, b2), next1 |.| next2)
      })

  // Sequential combination of MSFs. Allows to pass outputs of some MSF as input of some another MSF.
  let rec (>.>) ((MSFRL f1) : MSFRL<'r, 'a, 'b>) ((MSFRL f2) : MSFRL<'r, 'b, 'c>)
    : MSFRL<'r, 'a, 'c>
    = MSFRL (fun a -> monad {
        let! (b, next1) = f1 a
        let! (c, next2) = f2 b
        return (c, next1 >.> next2)
      })
  let rec (>.>!) ((MSF f1) : MSF<'a, 'b>) ((MSF f2) : MSF<'b, 'c>)
    : MSF<'a, 'c>
    = MSF (fun a ->
        let (b, next1) = f1 a
        let (c, next2) = f2 b
        (c, next1 >.>! next2)
      )

  // .|. is analogous to |.|, but broadcasts a single input to both MSF computations.
  let (.|.) (msf1 : MSFRL<'r, 'a, 'b1>) (msf2 : MSFRL<'r, 'a, 'b2>)
    : MSFRL<'r, 'a, 'b1 * 'b2>
    = dup () >.> (msf1 |.| msf2)

  // Read environmental variable, introduced by runReaderS
  let ask unit : MSFRL<'r, 'a, 'r>
    = arrM (konst ask)

  let arrId unit : MSFRL<'r, 'a, 'a>
    = arr id

  // extendI allows to execute some MSF and keep both input and output
  let rec extendI (MSF f : MSF<'a, 'b>) : MSF<'a, 'a * 'b>
    = MSF (fun a ->
        let (b, next) = f a
        ((a, b), extendI next))

  let extend (msf : MSFRL<'r, 'a, 'b>) : MSFRL<'r, 'a, 'a * 'b>
    = (arrId () .|. msf)

  // spawner allows some MSF over ListT (ReaderT r Identity) to create a new fork.
  let rec spawner (MSFRL currf : MSFRL<'r, 'a, 'b * option<MSFRL<'r, 'a, 'b>>>) : MSFRL<'r, 'a, 'b>
    = MSFRL (fun a -> monad {
      let! ((res1, maybenew), next1) = currf a
      let my = (res1, spawner next1)
      match maybenew with
      | None -> return my
      | Some (MSFRL newf) ->
        let (ListT n) = newf a
        return! ListT (Reader.map (fun x -> my :: x) n)
    })

  // switcher allows some MSF computation to switch to a new one at some point.
  let rec switcher (MSFRL currf : MSFRL<'r, 'a, Choice<'b, MSFRL<'r, 'a, 'b>>>) : MSFRL<'r, 'a, 'b>
    = MSFRL (fun a -> monad {
      let! (res, next) = currf a
      match res with
      | Choice1Of2 res -> return (res, switcher next)
      | Choice2Of2 (MSFRL next) -> return! next a
    })

  // hoist allows to alter monadic computation of inner MSF.
  // This is later used to locally alter environment provided by ReaderT.
  let rec hoist (f : 'c -> ListT<Reader<'r, list<'b * MSFRL<'r, 'a, 'b>>>> -> ListT<Reader<'r, list<'b * MSFRL<'r, 'a, 'b>>>>) (MSFRL currf : MSFRL<'r, 'a, 'b>) : MSFRL<'r, 'a * 'c, 'b>
    = MSFRL (fun (a, c) -> monad {
        let! (res, next) = f c (currf a)
        return (res, hoist f next)
      })

module Sim =
  open FRP

  // stepIntegral is a utility MSF that accepts dt and f(t+dt) as input and outputs an ∫f(t)dt from t to t+dt
  // assuming the transition from f(t) to f(t+dt) was linear.
  // This is a useful approximation to calculate update position of the object:
  // given object velocity on previous and current steps and dt, it is
  // possible to approximate distance traversed.
  let stepIntegral unit : MSFRL<'r, float32 * Vector3, Vector3>
    = mealy (fun (dt, newVal) oldVal -> 
        ((match oldVal with
          | None -> newVal
          | Some oldVal -> (newVal+oldVal) / 2f
        )*dt, Some newVal)
      ) None

  // Overridable environmental variables passed to all spheres.
  type Env = { mass : float32; bounciness : float32; dt : float32; gravity: float32 }

  // Result of computation
  type Obj = { index : int; impulse : Vector3; position : Vector3; toggled : bool }

  // Utility MSF. Accepts list of all collisions and returns true if given sphere collided with something.
  let detectHit (index : int) : MSFRL<'r, Map<int, Set<int>*Vector3>, bool>
    = mealy (fun collisions oldColl ->
        let newColl = Option.defaultValue Set.empty (Option.map fst <| Map.tryFind index collisions)
        (Set.count (Set.difference newColl oldColl) > 0, newColl))
        Set.empty

  // Base implementation of a sphere that carries no logic on collision.
  let simpleBall (index : int) (startPos : Vector3) : MSFRL<Env, Map<int, Set<int>*Vector3>, Obj*(Env -> Env)>
    = extend (ask ()) // Get environmental variables
    >.> extend (accumulateWith (fun (collisions, env) i -> // v Calculate new impulse based on gravity and collisions with other objects
        i
        |> ((+) (Vector3 (0f, -env.mass*env.gravity*env.dt, 0f)))
        |> Option.foldBack (fun (_, dimp) imp -> imp + dimp*(1f+env.bounciness)) (Map.tryFind index collisions)
      ) Vector3.zero)
    >.> arr (fun ((_, env), impulse) -> (env, impulse))
    >.> extend ( // v Calculate new position
      arr (fun (env, impulse) -> (env.dt, impulse/env.mass)) // dt, velocity
      >.> stepIntegral () // position delta
      >.> accumulateWith (fun dpos pos -> pos + dpos) startPos) // (dt, impulse), position
    >.> arr (fun ((_, impulse), position) -> { index = index; impulse = impulse; position = position; toggled = false }) // output
    >.> arr (fun res -> (res, id))

  // An extension to simpleBall. Executes simpleBall, on collision creates a ListT fork that executes simpleBall.
  let duplicatorBall (index : int) (startPos : Vector3) (usedIndices : int): MSFRL<Env, Map<int, Set<int>*Vector3>, Obj*(Env -> Env)>
    = spawner (
        (simpleBall index startPos .|. detectHit index)
        >.> mealy (fun (obj, hit) fresh ->
          if hit && fresh < usedIndices+8
            then ((obj, Some (simpleBall fresh ((fst obj).position + (Vector3(1f, 1f, 1f))))), fresh+1)
            else ((obj, None), fresh)
        ) usedIndices
      )

  // An extension to simpleBall. Stores mass and increases it on each hit. Uses ReaderT to execute simpleBall in updated context.
  let dynamicMassBall (index : int) (startPos : Vector3) : MSFRL<Env, Map<int, Set<int>*Vector3>, Obj*(Env -> Env)>
    = extend (detectHit index >.> accumulateWith (fun hit massFactor -> if hit then 2f*massFactor else massFactor) 1.0f)
    >.> hoist (fun massFactor comp -> ListT.Local (comp, fun env -> { env with mass = env.mass*massFactor })) (simpleBall index startPos)

  // An extension to simpleBall. Behaves like simpleBall, on hit switches to bounciness = 0.9f and executes simpleBall in updated context.
  let bouncyBall (index : int) (startPos : Vector3) : MSFRL<Env, Map<int, Set<int>*Vector3>, Obj*(Env -> Env)>
    = extend (switcher (
      detectHit index
      >.> arr (fun hit ->
        if hit
          then Choice2Of2 (arr (fun _ -> ((fun env -> { env with bounciness = 0.9f }) : Env -> Env)))
          else Choice1Of2 id)))
    >.> hoist (fun updater comp -> ListT.Local (comp, updater)) (simpleBall index startPos)

  // An extension to simpleBall. Stores gravity factor, updates it on each hit and extends output of simpleBall with update function that will
  // be applied to global context on the next step.
  let gravityChangerBall (index : int) (startPos : Vector3) : MSFRL<Env, Map<int, Set<int>*Vector3>, Obj*(Env -> Env)>
    = (simpleBall index startPos .|. (detectHit index >.> accumulateWith (fun hit gravityFactor -> if hit then 0.9f*gravityFactor else gravityFactor) 1.0f))
    >.> arr (fun ((ball, f), gravityFactor) -> (ball, fun env -> f <| { env with gravity = env.gravity * gravityFactor }))

  // An extension to simpleBall. Extends output object with "toggled" flag that switches each time the sphere collides with something.
  // This flag is later used to determine color of the sphere.
  let dynamicColorBall (index : int) (startPos : Vector3) : MSFRL<Env, Map<int, Set<int>*Vector3>, Obj*(Env -> Env)>
    = (simpleBall index startPos .|. (detectHit index >.> accumulateWith (fun hit toggled -> if hit then not toggled else toggled) false))
    >.> arr (fun ((ball, f), toggled) -> ({ ball with toggled = toggled }, f))

  // Impulse applied to colliding objects to separate them.
  let unstuckImpulse = 0.08f
  // Sphere radius
  let radius = 0.5f

  // Collision detection system. Accepts data about immovable barriers (such as floor, walls), a list of objects, and
  // returns list of collisions with force that needs to be appliedto respective objects.
  // This computation is not precise and not efficient, but could be improved without changing type.
  type WallData = { root : Vector3; normal: Vector3 }
  let collisions (walls : list<Vector3 * Vector3 * Vector3>) : MSF<list<Obj>, Map<int, Set<int>*Vector3>>
    = let walls : list<int * WallData>
        = List.indexed [ for (a, b, c) in walls -> { root = a; normal = Vector3.Normalize (Vector3.Cross (b - a, c - a)) }]
      let stopImpulse (objs : list<Obj>) (origObj : Obj) : Set<int>*Vector3
        = let (contacted, newImpulse) =
            (Set.empty, origObj.impulse)
            |> List.foldBack (fun (wallInd, wall) (contacted, impulse) ->
                if Vector3.Dot (origObj.position - wall.root, wall.normal) < radius
                  then (Set.add (-1-wallInd) contacted, impulse + (unstuckImpulse + max 0f (- (Vector3.Dot (origObj.impulse, wall.normal))))*wall.normal)
                  else (contacted, impulse)
              )
              walls
            |> List.foldBack (fun obj (contacted, impulse) ->
                if Vector3.Distance (obj.position, origObj.position) < 2f*radius
                  then (Set.add obj.index contacted, (Vector3.Normalize (origObj.position - obj.position))*unstuckImpulse + (impulse + obj.impulse)/2f)
                  else (contacted, impulse)
              )
              (List.filter (fun obj -> obj.index <> origObj.index) objs)
          (contacted, newImpulse - origObj.impulse)
      arrI (fun objs -> Map.ofList [ for obj in objs -> (obj.index, stopImpulse objs obj) ])

  // An actualy simulation. On each step, initializes ReaderT and ListT and runs all spheres in the simulation.
  // Collisions and updates to global environmet are passed onto subsequent simulation steps via feedbackI method.
  let simulation (pos : Vector3[]) (walls : list<Vector3 * Vector3 * Vector3>) : MSF<float32, list<Obj>>
    = feedbackI (Map.empty, []) (
        arrI (fun (x, (collisions, envUpds)) -> 
          let envUpd = List.fold (<<) id envUpds
          (envUpd { mass = 1f; bounciness = 0.5f; dt = x; gravity = 9.8f }, collisions))
        >.>! runReaderS (runListS (fromList
          [ simpleBall 0 pos[0]
          ; simpleBall 1 pos[1]
          ; duplicatorBall 2 pos[2] 7
          ; dynamicMassBall 3 pos[3]
          ; gravityChangerBall 4 pos[4]
          ; bouncyBall 5 pos[5]
          ; dynamicColorBall 6 pos[6]
          ]))
        >.>! arrI List.unzip
        >.>! extendI (arrI fst >.>! collisions walls)
        >.>! arrI (fun ((a, b), c) -> (a, (c, b)))
      )

type SimpleScript() =
  inherit MonoBehaviour()

  let mutable sim : FRP.MSF<float32, list<Sim.Obj>> = FRP.arrI (fun _ -> [])

  [<field:SerializeField>]
  member val balls : GameObject[] = [||] with get, set

  [<field:SerializeField>]
  member val walls : Vector3[] = [||] with get, set

  member self.Start() =
    // Initates the simulation from Sim.simulation function and initial Unity Engine scene.
    sim <- Sim.simulation
      [|for ball in self.balls -> ball.transform.position|]
      [for i in 0..((Array.length self.walls)/3 - 1) -> self.walls[3*i], self.walls[3*i+1], self.walls[3*i+2]]
    
  member self.Update() =
    // Runs the simulation
    let (FRP.MSF step) = sim
    let (objs, next) = step Time.deltaTime
    for obj in objs do
      // Reads result of the simulation and performs required update to objects on screen via Unity Engine.
      if obj.index >= Array.length self.balls
        then self.balls <- Array.append self.balls [|GameObject.CreatePrimitive (PrimitiveType.Sphere)|]
      self.balls[obj.index].transform.position <- obj.position
      self.balls[obj.index].GetComponent<Renderer>().material.color <- if obj.toggled then Color.red else Color.white
    // Executing MSF computation yields new MSF computation to execute on following steps. We save for use in later iterations.
    sim <- next

