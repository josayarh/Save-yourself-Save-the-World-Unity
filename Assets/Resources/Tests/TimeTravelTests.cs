    using System;
    using System.Collections;
using System.Collections.Generic;
    using System.Threading;
    using FLFlight;
    using NUnit.Framework;
using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;

namespace Tests
{
    public class TimeTravelTests
    {
        [UnityTest]
        public IEnumerator TestPlayerDeath()
        {
            yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Single);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("TestScene"));

            yield return new WaitUntil(() => Time.frameCount > 1);
            GameObject player = GameObject.Find("Player(Clone)");
            yield return new WaitUntil(() => player == null);
            
            yield return SceneManager.UnloadSceneAsync("TestScene");
        }
        
        [UnityTest]
        public IEnumerator TestShipCreation()
        {
            yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Single);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("TestScene"));

            // Test for player death 
            yield return new WaitUntil(() => Time.frameCount > 1);
            GameObject player = GameObject.Find("Player(Clone)");
            Assert.AreNotEqual( player, null);
            PlayerSave playerSave = player.GetComponentInChildren<PlayerSave>();
            Assert.AreNotEqual( playerSave, null);
            Guid playerId = playerSave.Id;
            Vector3 playerPosition = player.transform.position;
            yield return new WaitUntil(() => player == null);

            int frameCount = Time.frameCount;
            
            // Wait some time for the game to reboot
            yield return new WaitUntil(() => Time.frameCount > frameCount + 5);
            
            // Check if the player ghost was properly created
            Assert.Greater( Pool.Instance.PlayerBotList.Count, 0);
            GameObject playerBot = Pool.Instance.PlayerBotList[0];
            PlayerBotController playerBotController = playerBot.GetComponent<PlayerBotController>();
            Assert.AreEqual( playerId, playerBotController.Id);
            Assert.AreEqual(playerPosition, playerBot.transform.position);
                
            yield return SceneManager.UnloadSceneAsync("TestScene");
        }

        [UnityTest]
        public IEnumerator TestEnemyDeathAndRespawn()
        {
            yield return SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Single);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("TestScene"));
            
            //look for enemy further away from player 
            yield return new WaitUntil(() => Time.frameCount > 1);
            float maxDistance = 0;
            GameObject furtherEnemy = null; 
            foreach (GameObject enemy in Pool.Instance.EnemyList)
            {
                float distance = Vector3.Distance(enemy.transform.position,
                    Ship.PlayerShip.gameObject.transform.position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    furtherEnemy = enemy;
                }
            }

            //Kill that enemy 
            Assert.AreNotEqual(furtherEnemy, null);
            EnemyController enemyController = furtherEnemy.GetComponent<EnemyController>();
            Guid enemyId = enemyController.Id;
            Assert.AreNotEqual(enemyController, null);
            enemyController.Destroy();
            
            //Wait for scene to reboot 
            GameObject player = GameObject.Find("Player(Clone)");
            yield return new WaitUntil(() => player == null);
            int rebootFrame = Time.frameCount;
            
            //Check if enemy has returned in bot mode
            yield return new WaitUntil(() => Time.frameCount >rebootFrame + 1);
            maxDistance = 0;
            furtherEnemy = null; 
            foreach (GameObject enemy in Pool.Instance.EnemyList)
            {
                float distance = Vector3.Distance(enemy.transform.position,
                    Ship.PlayerShip.gameObject.transform.position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    furtherEnemy = enemy;
                }
            }
            
            Assert.AreNotEqual(furtherEnemy, null);
            enemyController = furtherEnemy.GetComponent<EnemyController>();
            Assert.AreNotEqual(enemyController, null);
            Assert.AreEqual(enemyController.IsAiOn, false);
            Assert.AreEqual(enemyController.Id, enemyId);
            
            yield return SceneManager.UnloadSceneAsync("TestScene");
        }
    }
}
