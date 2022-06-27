
# EndlessCloudExam

1. As a player, on key press (AWSD), I can move the player in any direction.
    - [x] Player move by Tanslate, rotate by assign rotate value.

2. As a player, per each key press (P), I can use the sword provided to attack once onto any placeholder objects such as a cube.
    - [x] I set the combo mechanism to attack. Press p again, the second attack will be triggered. And the enemy will receive the attack through Boxcast.

3. As a player, I can take damage if I get hit by placeholder projectiles (sphere) that get fired towards the player by the enemy once every 3 seconds.
    - [x] I use the object pool to generate bullets, and use the RayCast to determine the hit, avoid the bullet goes through wall. <br/>
          After the hit is determined, it will cause damage to the player.

4. As a player, on key press (SPACE), I can perform a dodge in the direction where the character is facing to avoid damage.
    - [x] I declare a dictionary of priorities to make behavior can be sequenced. I prioritize "dodge" over "damage", to avoid damage being triggered.

5. As a player, after 5 hits, the placeholder enemy will die (disappear) and respawn at a random location within a radius of 3.
    - [x] I set the enemy's HP to 5 to make sure it disappears after 5 player attacks, and made Spawner to spawn new enemies within 3 of the player's radius after 0.5 seconds

> In addition, I have set two hotkeys, F1 and F2.
> <br/>
> F1 is used to turn on/off the backswing animation of dodge.
> <br/>
> F2 is used to turn on/off the backswing animation of the attack.
