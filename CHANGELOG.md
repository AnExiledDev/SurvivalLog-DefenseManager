# Changelog

## 0.4.0

The Mechanical Chainsaw now hits crawler zombies. In the base game they dodge it every time, so its blade never hits them. Now they dodge it 25% of the time by default. New config settings `CrawlerDodgeChance` and `ArmoredCrawlerDodgeChance` set the chance for each kind of crawler, from 0 (every hit lands) to 1 (the game's own behavior).

## 0.3.3

Repair and Repair all no longer assume every repair costs 10 stamina. Repairs often cost less, so Repair all used to stop short. Now repairs are only held back when you have less than the 10 stamina a repair needs to start, and not at all when actions you queued run first. If your stamina runs out partway, the game refuses the rest.

## 0.3.2

First public release. One panel for the traps and barricades outside your home. Repair all damaged defenses in one press, clear the broken ones, or repair and remove them one by one. Opens with F7 or a HUD button, with Repair all and Clear all broken also on the game's Layout screen. Switches electrical defenses on only while zombies are at the base, only when zombies come near, or always.
