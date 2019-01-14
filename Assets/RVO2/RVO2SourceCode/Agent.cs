/*
 * Agent.cs
 * RVO2 Library C#
 *
 * Copyright 2008 University of North Carolina at Chapel Hill
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */

//using System;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RVO
{
    /**
     * <summary>Defines an agent in the simulation.</summary>
     */
    internal class Agent
    {
        // float,表示距离
        internal IList<KeyValuePair<float, Agent>> agentNeighbors_ = new List<KeyValuePair<float, Agent>>();
        // 附近的静态障碍物
        internal IList<KeyValuePair<float, Obstacle>> obstacleNeighbors_ = new List<KeyValuePair<float, Obstacle>>();
        internal IList<Line> orcaLines_ = new List<Line>();
        internal Vector2 position_;
        internal Vector2 prefVelocity_;
        internal Vector2 velocity_;

        internal int id_ = 0;

        // agent参考的最大临近agent数量
        internal int maxNeighbors_ = 0;
        // agent的最大速度
        internal float maxSpeed_ = 0.0f;
        // 被认为是agent附近的距离
        internal float neighborDist_ = 0.0f;
        // agent简化成一个圆，radius表示半径
        internal float radius_ = 0.0f;
        // 预测agent之间发生碰撞的时间
        internal float timeHorizon_ = 0.0f;
        // 预测agent与obstacle发生碰撞的时间
        internal float timeHorizonObst_ = 0.0f;

        private Vector2 newVelocity_;

        #region fzy add：
        internal float radius_default;
        internal Vector3 origin;
        internal Vector3 goal;
        internal float positionY;
        internal Vector3 position_v3;
        internal Quaternion rotation;
        internal GPUSkinningController gpuSkinningController;

        internal bool isActive;
        Vector3 goalVector;
        float distance_ChangeVelocity = 5.0f;

        // 一些常量
        float arriveDistance = 0.25f;
        System.Random rnd = new System.Random();
        #endregion
        /// <summary>
        /// fzy add:设置prefVelocity
        /// </summary>
        internal void setPrefVelocity()
        {
            goalVector = goal - position_v3;
            // 速度方向变化
            if (goalVector.magnitude > distance_ChangeVelocity)
            {
                float angle = ((float)rnd.NextDouble() - 0.5f) * 60.0f;
                goalVector = Quaternion.Euler(0.0f, angle, 0.0f) * goalVector;

                radius_ = radius_default;
            }
            else
                radius_ = radius_default / 2;
            goalVector *= 2;
            //if (RVOMath.absSq(goalVector) > 1.0f)
            // fzy modify: 速度限制在0.0f~agentMaxSpeed
            if (goalVector.magnitude > maxSpeed_)
            {
                goalVector = goalVector.normalized * maxSpeed_;
            }

            prefVelocity_ = RVOMath.V3ToV2(goalVector);
        }

        #region ORCA 计算函数
        /**
         * <summary>Computes the neighbors of this agent.</summary>
         */
        internal void computeNeighbors()
        {
            // 检测附近障碍物
            obstacleNeighbors_.Clear();
            float rangeSq = RVOMath.sqr(timeHorizonObst_ * maxSpeed_ + radius_);
            Simulator.Instance.kdTree_.computeObstacleNeighbors(this, rangeSq);

            agentNeighbors_.Clear();

            // 检测附近Agent
            if (maxNeighbors_ > 0)
            {
                rangeSq = RVOMath.sqr(neighborDist_);
                Simulator.Instance.kdTree_.computeAgentNeighbors(this, ref rangeSq);
            }
        }

        /**
         * <summary>Computes the new velocity of this agent.</summary>
         */
        internal void computeNewVelocity()
        {
            orcaLines_.Clear();

            #region obstacle ORCA
            float invTimeHorizonObst = 1.0f / timeHorizonObst_;

            /* Create obstacle ORCA lines. */
            for (int i = 0; i < obstacleNeighbors_.Count; ++i)
            {

                Obstacle obstacle1 = obstacleNeighbors_[i].Value;
                Obstacle obstacle2 = obstacle1.next_;

                Vector2 relativePosition1 = obstacle1.point_ - position_;
                Vector2 relativePosition2 = obstacle2.point_ - position_;

                /*
                 * Check if velocity obstacle of obstacle is already taken care
                 * of by previously constructed obstacle ORCA lines.
                 */
                // fzy remark：如果当前ORCA Line已经被其他障碍物所覆盖，则不用重复计算
                bool alreadyCovered = false;

                for (int j = 0; j < orcaLines_.Count; ++j)
                {
                    if (RVOMath.det(invTimeHorizonObst * relativePosition1 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON 
                        && RVOMath.det(invTimeHorizonObst * relativePosition2 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON)
                    {
                        alreadyCovered = true;

                        break;
                    }
                }

                if (alreadyCovered)
                {
                    continue;
                }

                /* Not yet covered. Check for collisions. */
                float distSq1 = RVOMath.absSq(relativePosition1);
                float distSq2 = RVOMath.absSq(relativePosition2);
                float radiusSq = RVOMath.sqr(radius_);

                Vector2 obstacleVector = obstacle2.point_ - obstacle1.point_;
                //float s = (-relativePosition1 * obstacleVector) / RVOMath.absSq(obstacleVector);
                float s = Vector2.Dot(-relativePosition1, obstacleVector) / RVOMath.absSq(obstacleVector);
                //计算agent到obstacle直线（非线段）的距离
                float distSqLine = RVOMath.absSq(-relativePosition1 - s * obstacleVector);

                Line line;

                // fzy remark:agent已经和obstacle相交，且在obstacle线的左侧
                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    /* Collision with left vertex. Ignore if non-convex. */
                    // fzy remark:凹顶点被其他凸顶点挡住（凸顶点所在的obstacle线把凹顶点所在的obstacle线遮挡）
                    if (obstacle1.convex_)
                    {
                        // 为什么是(0,0)？因为Agent已经和Obstacle碰撞？
                        line.point = new Vector2(0.0f, 0.0f);
                        line.direction = RVOMath.normalize(new Vector2(-relativePosition1.y, relativePosition1.x));
                        orcaLines_.Add(line);
                    }

                    continue;
                }
                // fzy remark:agent已经和obstacle相交，且在obstacle线的右侧
                else if (s > 1.0f && distSq2 <= radiusSq)
                {
                    /*
                     * Collision with right vertex. Ignore if non-convex or if
                     * it will be taken care of by neighboring obstacle.
                     */
                     // fzy remark:1.凹顶点不考虑。2.凸顶点 且 obstacle2方向（下一个计算的障碍物）和agent方向夹角大于90度，不考虑。
                     // 大于90度的先不考虑，在计算obstacle2的时候再计算，避免重复计算(在277行fzy remark)
                    if (obstacle2.convex_ && RVOMath.det(relativePosition2, obstacle2.direction_) >= 0.0f)
                    {
                        line.point = new Vector2(0.0f, 0.0f);
                        line.direction = RVOMath.normalize(new Vector2(-relativePosition2.y, relativePosition2.x));
                        orcaLines_.Add(line);
                    }

                    continue;
                }
                // fzy remark:agent已经和obstacle相交，且在obstacle线的中间
                else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                {
                    /* Collision with obstacle segment. */
                    line.point = new Vector2(0.0f, 0.0f);
                    // fzy remark：注意方向
                    line.direction = -obstacle1.direction_;
                    orcaLines_.Add(line);

                    continue;
                }

                /*
                 * No collision. Compute legs. When obliquely viewed, both legs
                 * can come from a single vertex. Legs extend cut-off line when
                 * non-convex vertex.
                 */
                // fzy remark:leg->以agent为圆心，radius为半径画圆，圆外目标点（obstacle1或obstacle2）作该圆的2条切线，获得2个切点
                // 从切点指向圆外目标点的方向向量，即为leg
                Vector2 leftLegDirection, rightLegDirection;

                // fzy remark：切线在圆弧上
                if (s < 0.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that left vertex
                     * defines velocity obstacle.
                     */
                    // fzy remark：凹节点不考虑
                    if (!obstacle1.convex_)
                    {
                        /* Ignore obstacle. */
                        continue;
                    }
                    obstacle2 = obstacle1;
                    
                    float leg1 = RVOMath.sqrt(distSq1 - radiusSq);
                    // cos(a+b)=cosa*cosb-sina*sinb;sin(a+b)=sina*cosb+cosa*sinb;
                    leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, 
                        relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                    // cos(a-b)=cosa*cosb+sina*sinb;sin(a-b)=sina*cosb-sinb*cosa;
                    rightLegDirection = new Vector2(relativePosition1.x * leg1 + relativePosition1.y * radius_, 
                        -relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                }
                // 切线在圆弧上
                else if (s > 1.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that
                     * right vertex defines velocity obstacle.
                     */
                    if (!obstacle2.convex_)
                    {
                        /* Ignore obstacle. */
                        continue;
                    }

                    obstacle1 = obstacle2;

                    float leg2 = RVOMath.sqrt(distSq2 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition2.x * leg2 - relativePosition2.y * radius_, 
                        relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                    rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius_, 
                        -relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                }
                else
                {
                    /* Usual situation. */
                    if (obstacle1.convex_)
                    {
                        float leg1 = RVOMath.sqrt(distSq1 - radiusSq);
                        leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, 
                            relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                    }
                    else
                    {
                        /* Left vertex non-convex; left leg extends cut-off line. */
                        // 凹节点在obstacle内，考虑obstacle1的相反方向向量作为ORCA Line
                        leftLegDirection = -obstacle1.direction_;
                    }

                    if (obstacle2.convex_)
                    {
                        float leg2 = RVOMath.sqrt(distSq2 - radiusSq);
                        rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius_, 
                            -relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                    }
                    else
                    {
                        /* Right vertex non-convex; right leg extends cut-off line. */
                        // 凹节点会穿过obstacle，直接使用凹节点线段的正方向
                        rightLegDirection = obstacle1.direction_;
                    }
                }

                /*
                 * Legs can never point into neighboring edge when convex
                 * vertex, take cutoff-line of neighboring edge instead. If
                 * velocity projected on "foreign" leg, no constraint is added.
                 */

                // 寻找velocity_距离VO区域边界最近的点
                // ORCA Line.Point = 最近点
                // ORCA Line.Direction = VO区域在最近点的切线
                Obstacle leftNeighbor = obstacle1.previous_;

                bool isLeftLegForeign = false;
                bool isRightLegForeign = false;

                // leg指向临近obstacle线段内
                // fzy remark:agent在leftNeighbor的右侧
                //（leftNeighbor方向（下一个计算的障碍物）和agent方向夹角大于90度）
                if (obstacle1.convex_ && RVOMath.det(leftLegDirection, -leftNeighbor.direction_) >= 0.0f)
                {
                    /* Left leg points into obstacle. */
                    leftLegDirection = -leftNeighbor.direction_;
                    isLeftLegForeign = true;
                }

                if (obstacle2.convex_ && RVOMath.det(rightLegDirection, obstacle2.direction_) <= 0.0f)
                {
                    /* Right leg points into obstacle. */
                    rightLegDirection = obstacle2.direction_;
                    isRightLegForeign = true;
                }

                /* Compute cut-off centers. */
                // fzy remark:注意obstacle1和obstacle2可能已经发生变化
                Vector2 leftCutOff = invTimeHorizonObst * (obstacle1.point_ - position_);
                Vector2 rightCutOff = invTimeHorizonObst * (obstacle2.point_ - position_);
                Vector2 cutOffVector = rightCutOff - leftCutOff;

                /* Project current velocity on velocity obstacle. */

                /* Check if current velocity is projected on cutoff circles. */
                float t = obstacle1 == obstacle2 ? 0.5f : Vector2.Dot(velocity_ - leftCutOff,cutOffVector) / RVOMath.absSq(cutOffVector);
                float tLeft = Vector2.Dot(velocity_ - leftCutOff, leftLegDirection);
                float tRight = Vector2.Dot(velocity_ - rightCutOff, rightLegDirection);

                if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
                {
                    /* Project on left cut-off circle. */
                    Vector2 unitW = RVOMath.normalize(velocity_ - leftCutOff);

                    line.direction = new Vector2(unitW.y, -unitW.x);
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * unitW;
                    orcaLines_.Add(line);

                    continue;
                }
                else if (t > 1.0f && tRight < 0.0f)
                {
                    /* Project on right cut-off circle. */
                    Vector2 unitW = RVOMath.normalize(velocity_ - rightCutOff);

                    line.direction = new Vector2(unitW.y, -unitW.x);
                    line.point = rightCutOff + radius_ * invTimeHorizonObst * unitW;
                    orcaLines_.Add(line);

                    continue;
                }

                /*
                 * Project on left leg, right leg, or cut-off line, whichever is
                 * closest to velocity.
                 */
                float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : RVOMath.absSq(velocity_ - (leftCutOff + t * cutOffVector));
                float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : RVOMath.absSq(velocity_ - (leftCutOff + tLeft * leftLegDirection));
                float distSqRight = tRight < 0.0f ? float.PositiveInfinity : RVOMath.absSq(velocity_ - (rightCutOff + tRight * rightLegDirection));

                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    /* Project on cut-off line. */
                    line.direction = -obstacle1.direction_;
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
                    orcaLines_.Add(line);

                    continue;
                }

                if (distSqLeft <= distSqRight)
                {
                    /* Project on left leg. */
                    // 左投射到上一条Obstacle Line（已在之前的Obstacle计算，不重复添加Line）
                    if (isLeftLegForeign)
                    {
                        continue;
                    }

                    line.direction = leftLegDirection;
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
                    orcaLines_.Add(line);

                    continue;
                }

                /* Project on right leg. */
                // 右投射到下一条Obstacle Line（将在之后的Obstacle计算，这里先不添加）
                if (isRightLegForeign)
                {
                    continue;
                }

                line.direction = -rightLegDirection;
                line.point = rightCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
                orcaLines_.Add(line);
            }
#endregion

            #region Agent ORCA
            int numObstLines = orcaLines_.Count;

            float invTimeHorizon = 1.0f / timeHorizon_;

            /* Create agent ORCA lines. */
            for (int i = 0; i < agentNeighbors_.Count; ++i)
            {
                Agent other = agentNeighbors_[i].Value;

                // fzy remark：位置差向量和速度差向量的方向是相反的
                Vector2 relativePosition = other.position_ - position_;
                Vector2 relativeVelocity = velocity_ - other.velocity_;
                float distSq = RVOMath.absSq(relativePosition);
                float combinedRadius = radius_ + other.radius_;
                float combinedRadiusSq = RVOMath.sqr(combinedRadius);

                Line line;
                Vector2 u;

                if (distSq > combinedRadiusSq)
                {
                    /* No collision. */
                    // w是速度向量，或者经过单位时间后，agent和other agent的相对位置向量
                    Vector2 w = relativeVelocity - invTimeHorizon * relativePosition;

                    /* Vector from cutoff center to relative velocity. */
                    // fzy remark:cutoff center 即为 invTimeHorizon * relativePosition
                    float wLengthSq = RVOMath.absSq(w);
                    float dotProduct1 = Vector2.Dot(w, relativePosition);

                    // 相对速度脱离VO区域的最近点在圆上
                    if (dotProduct1 < 0.0f && RVOMath.sqr(dotProduct1) > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        float wLength = RVOMath.sqrt(wLengthSq);
                        Vector2 unitW = w / wLength;

                        // 脱离点（最近点）位于圆弧上，u向量为该圆弧的法线
                        line.direction = new Vector2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    // 相对速度脱离VO区域的最近点在圆的切线（leg）上
                    else
                    {
                        /* Project on legs. */
                        float leg = RVOMath.sqrt(distSq - combinedRadiusSq);

                        // 行列式大于0，将ORCA Line的方向向量投射到左腿
                        if (RVOMath.det(relativePosition, w) > 0.0f)
                        {
                            /* Project on left leg. */
                            // 利用合角公式计算切线单位向量
                            line.direction = new Vector2(relativePosition.x * leg - relativePosition.y * combinedRadius, 
                                relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }
                        // 否则，将ORCA Line的方向向量投射到右腿
                        else
                        {
                            /* Project on right leg. */
                            // 利用合角公式计算切线单位向量
                            line.direction = -new Vector2(relativePosition.x * leg + relativePosition.y * combinedRadius, 
                                -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }

                        // 计算u：相对速度向量到VO边缘的最短向量
                        // n：u到VO最近边缘点的切线向量
                        float dotProduct2 = Vector2.Dot(relativeVelocity, line.direction);
                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                // 已经相撞
                else
                {
                    /* Collision. Project on cut-off circle of time timeStep. */
                    // 这里是timeStep，并非预测时间timeHorizon
                    float invTimeStep = 1.0f / Simulator.Instance.timeStep_;

                    /* Vector from cutoff center to relative velocity. */
                    Vector2 w = relativeVelocity - invTimeStep * relativePosition;

                    float wLength = RVOMath.abs(w);
                    Vector2 unitW = w / wLength;

                    line.direction = new Vector2(unitW.y, -unitW.x);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                // 每个agent各自承担一半的避让行为
                line.point = velocity_ + 0.5f * u;
                orcaLines_.Add(line);
            }
            #endregion

            // 使用二维线性规划求解，lineFail表示线性规划满足的约束数量
            int lineFail = linearProgram2(orcaLines_, maxSpeed_, prefVelocity_, false, ref newVelocity_);

            // 线性规划满足的约束数量小于总约束数量，未求得全局最优解（newVelocity_仅求得局部最优解）
            if (lineFail < orcaLines_.Count)
            {
                linearProgram3(orcaLines_, numObstLines, lineFail, maxSpeed_, ref newVelocity_);
            }
        }
        #endregion
        /**
         * <summary>Inserts an agent neighbor into the set of neighbors of this
         * agent.</summary>
         *
         * <param name="agent">A pointer to the agent to be inserted.</param>
         * <param name="rangeSq">The squared range around this agent.</param>
         */
        internal void insertAgentNeighbor(Agent agent, ref float rangeSq)
        {
            if (this != agent)
            {
                float distSq = RVOMath.absSq(position_ - agent.position_);

                if (distSq < rangeSq)
                {
                    if (agentNeighbors_.Count < maxNeighbors_)
                    {
                        agentNeighbors_.Add(new KeyValuePair<float, Agent>(distSq, agent));
                    }

                    // 按距离从小到大排序
                    int i = agentNeighbors_.Count - 1;

                    while (i != 0 && distSq < agentNeighbors_[i - 1].Key)
                    {
                        agentNeighbors_[i] = agentNeighbors_[i - 1];
                        --i;
                    }

                    agentNeighbors_[i] = new KeyValuePair<float, Agent>(distSq, agent);

                    // 更新检测的最大范围
                    if (agentNeighbors_.Count == maxNeighbors_)
                    {
                        rangeSq = agentNeighbors_[agentNeighbors_.Count - 1].Key;
                    }
                }
            }
        }

        /**
         * <summary>Inserts a static obstacle neighbor into the set of neighbors
         * of this agent.</summary>
         *
         * <param name="obstacle">The number of the static obstacle to be
         * inserted.</param>
         * <param name="rangeSq">The squared range around this agent.</param>
         */
        internal void insertObstacleNeighbor(Obstacle obstacle, float rangeSq)
        {
            Obstacle nextObstacle = obstacle.next_;

            float distSq = RVOMath.distSqPointLineSegment(obstacle.point_, nextObstacle.point_, position_);

            // agent到障碍物的距离小于range
            if (distSq < rangeSq)
            {
                // 在obstacle队列中添加新的obstacle
                obstacleNeighbors_.Add(new KeyValuePair<float, Obstacle>(distSq, obstacle));

                // obstacle距离从小到大排序
                int i = obstacleNeighbors_.Count - 1;

                while (i != 0 && distSq < obstacleNeighbors_[i - 1].Key)
                {
                    obstacleNeighbors_[i] = obstacleNeighbors_[i - 1];
                    --i;
                }
                obstacleNeighbors_[i] = new KeyValuePair<float, Obstacle>(distSq, obstacle);
            }
        }


        DateTime lastDT = DateTime.Now;
        DateTime curDT;
        /**
         * <summary>Updates the two-dimensional position and two-dimensional
         * velocity of this agent.</summary>
         */
        Quaternion targetQuaternion;
        internal void update()
        {
            curDT = DateTime.Now;
            float deltaTime = (float)(curDT.Subtract(lastDT)).TotalSeconds;
            lastDT = curDT;
            // 如果发生碰撞，导致transform.position != position_，需要更新position_（该功能尚未添加）
            velocity_ = newVelocity_;
            //position_ += velocity_ * Simulator.Instance.timeStep_;
            position_ += velocity_ * deltaTime;
            position_v3 = RVOMath.V2ToV3(position_, positionY);

            if (Vector3.Distance(position_v3, goal) < arriveDistance && gpuSkinningController.playingClip.name == "Walk")
            {
                gpuSkinningController.Play("Idle");
            }
            else if(Vector3.Distance(position_v3, goal) > arriveDistance*2 && gpuSkinningController.playingClip.name == "Idle")
            {
                gpuSkinningController.Play("Walk");
            }

            // 应用转向
            if (gpuSkinningController.playingClip.name != "Idle")
            {

                //fzy add(18.5.5):velocity_ = Vector2.zero ？
                if (velocity_ == Vector2.zero)
                {
                    Debug.Log("fzy debug:" + prefVelocity_);
                    return;
                }
                //targetQuaternion = Quaternion.FromToRotation(Vector3.forward, RVOMath.V2ToV3(velocity_.normalized));
                //targetQuaternion.x = 0f; targetQuaternion.z = 0f;
                targetQuaternion = Quaternion.LookRotation(RVOMath.V2ToV3(velocity_.normalized));
                rotation = Quaternion.Slerp(rotation, targetQuaternion, 0.1f);
            }
            else
            {
                //targetQuaternion = Quaternion.FromToRotation(Vector3.forward, RVOMath.V2ToV3((goal - origin).normalized));
                //targetQuaternion.x = 0f; targetQuaternion.z = 0f;
                targetQuaternion = Quaternion.LookRotation((goal - origin).normalized);
                rotation = Quaternion.Slerp(rotation, targetQuaternion, 0.1f);
            }
        }

        /**
         * <summary>Solves a one-dimensional linear program on a specified line
         * subject to linear constraints defined by lines and a circular
         * constraint.</summary>
         *
         * <returns>True if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="lineNo">The specified line constraint.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private bool linearProgram1(IList<Line> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            // fzy remark:从(0,0)指向lines[lineNo].point向量，在射线lines[linesNo].direction的投影
            float dotProduct = Vector2.Dot(lines[lineNo].point, lines[lineNo].direction);
            // 判别值：计算ORCA约束线是否穿过约束圆（radius/MaxSpeed）内（勾股定理）
            float discriminant = RVOMath.sqr(dotProduct) + RVOMath.sqr(radius) - RVOMath.absSq(lines[lineNo].point);

            // 小于0，表示lines[lineNo]在最大速度圆外（无相交区域）
            // 隐形条件：result、optVelocity.magnitude<=radius
            // 执行linearProgram1的条件判断
            if (discriminant < 0.0f)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            // fzy remark:lines[lineNO]与约束圆（radius/MaxSpeed）的左右两交点
            float sqrtDiscriminant = RVOMath.sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                // 分母和分子
                // lines[lineNo]与lines[i]夹角的sin值
                float denominator = RVOMath.det(lines[lineNo].direction, lines[i].direction);
                // 
                float numerator = RVOMath.det(lines[i].direction, lines[lineNo].point - lines[i].point);

                // 分母等于0（lines[i]与lines[linesNo]平行且方向相同）
                if (RVOMath.fabs(denominator) <= RVOMath.RVO_EPSILON)
                {
                    /* Lines lineNo and i are (almost) parallel. */
                    // 分子小于0表示lines[i]的覆盖区域比lines[lineNo]大
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                //t:lineNo和i的交点到point的距离
                float t = numerator / denominator;

                // 根据lineNo和i的交点，调整tLeft和tRight
                if (denominator >= 0.0f)
                {
                    /* Line i bounds line lineNo on the right. */
                    tRight = Mathf.Min(tRight, t);
                }
                else
                {
                    /* Line i bounds line lineNo on the left. */
                    tLeft = Mathf.Max(tLeft, t);
                }

                // lineNo和i的交点不在圆内
                if (tLeft > tRight)
                {
                    return false;
                }
            }

            // 方向优化（根据pref速度的方向计算result，只取两端极值）
            if (directionOpt)
            {
                /* Optimize direction. */
                // pref速度与约束线的夹角小于90度，取右极值为result
                if (Vector2.Dot(optVelocity, lines[lineNo].direction) > 0.0f)
                {
                    /* Take right extreme. */
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                // pref速度与约束线的夹角大于等于90度，取左极值为result
                else
                {
                    /* Take left extreme. */
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
            }
            // 方向不优化（寻找速度变化最小的点）
            else
            {
                /* Optimize closest point. */
                float t = Vector2.Dot(lines[lineNo].direction, optVelocity - lines[lineNo].point);

                // 根据optVelocity投射到lineNo上的距离，设置result
                if (t < tLeft)
                {
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                // optVelocity在tLeft和tRight中间
                else
                {
                    result = lines[lineNo].point + t * lines[lineNo].direction;
                }
            }

            return true;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <returns>The number of the line it fails on, and the number of lines
         * if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        // 返回值：成功的线性约束条件数量
        private int linearProgram2(IList<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            // optVelocity只是归一化的速度方向
            if (directionOpt)
            {
                /*
                 * Optimize direction. Note that the optimization velocity is of
                 * unit length in this case.
                 */
                //result = optVelocity * radius;
                result = optVelocity.normalized * radius;
            }
            // 速度大小限制在MaxSpeed内
            else if(optVelocity.magnitude > radius)
            //Author Version：增加了不必要的计算复杂度
            //else if (RVOMath.absSq(optVelocity) > RVOMath.sqr(radius))
            {
                /* Optimize closest point and outside circle. */
                result = RVOMath.normalize(optVelocity) * radius;
            }
            else
            {
                /* Optimize closest point and inside circle. */
                result = optVelocity;
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                // 当前result在ORCA约束线的逆时针一侧（不满足该约束线），重新计算result
                if (RVOMath.det(lines[i].direction, lines[i].point - result) > 0.0f)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    Vector2 tempResult = result;
                    if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                    {
                        // 找不到最优解，返回之前的result（局部最优解），以及当前约束线的索引
                        result = tempResult;

                        // i表示已经成功的线性规划约束数量
                        return i;
                    }
                }
            }

            return lines.Count;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="numObstLines">Count of obstacle lines.</param>
         * <param name="beginLine">The line on which the 2-d linear program
         * failed.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private void linearProgram3(IList<Line> lines, int numObstLines, int beginLine, float radius, ref Vector2 result)
        {
            float distance = 0.0f;

            for (int i = beginLine; i < lines.Count; ++i)
            {
                // 计算result点到lines[i]的垂直距离
                // result在lines[i]区域内，distance为负值，满足约束条件，直接进行下一次循环
                // result在lines[i]区域外，distance为正值，不满足约束条件。
                // 若result到lines[i]的距离小于distance，表示lines[i]并不是当前影响result最大的约束（之前已有更大的约束并已经进行计算），直接进行下一次循环
                if (RVOMath.det(lines[i].direction, lines[i].point - result) > distance)
                {
                    /* Result does not satisfy constraint of line i. */
                    IList<Line> projLines = new List<Line>();
                    // obstacle line 约束条件不松弛
                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projLines.Add(lines[ii]);
                    }

                    // agent line 约束条件需要松弛
                    for (int j = numObstLines; j < i; ++j)
                    {
                        Line line;

                        // line[i]和line[j]夹角的sin值
                        float determinant = RVOMath.det(lines[i].direction, lines[j].direction);

                        // line[i]平行line[j]（相同方向或相反）
                        if (RVOMath.fabs(determinant) <= RVOMath.RVO_EPSILON)
                        {
                            /* Line i and line j are parallel. */
                            // line[i].direction和line[j].direction相等，不用考虑line[j]
                            if (Vector2.Dot(lines[i].direction, lines[j].direction) > 0.0f)
                            {
                                /* Line i and line j point in the same direction. */
                                continue;
                            }
                            // line[i].direction和line[j].direction相反，line为lines[i].point和lines[j].point的中点
                            // 只有平行才可以取中点
                            // fzy remark：平行无法求交点，通过中点求出line.point
                            else
                            {
                                /* Line i and line j point in opposite direction. */
                                line.point = 0.5f * (lines[i].point + lines[j].point);
                            }
                        }
                        else
                        {
                            // 求lines[i]和lines[j]的交点作为line.point
                            // (RVOMath.det(lines[j].direction, lines[i].point - lines[j].point):lines[i].point到lines[j]的垂直距离
                            // determinant:lines[i]和lines[j]夹角的sin值
                            line.point = lines[i].point + 
                                (RVOMath.det(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                        }

                        line.direction = RVOMath.normalize(lines[j].direction - lines[i].direction);
                        projLines.Add(line);
                    }

                    Vector2 tempResult = result;
                    if (linearProgram2(projLines, radius, new Vector2(-lines[i].direction.y, lines[i].direction.x), true, ref result) < projLines.Count)
                    {
                        // 理论上这种情况不会发生，可能会由于float计算，或者agent微小的随机浮动造成
                        /*
                         * This should in principle not happen. The result is by
                         * definition already in the feasible region of this
                         * linear program. If it fails, it is due to small
                         * floating point error, and the current result is kept.
                         */
                        result = tempResult;
                    }

                    // 更新最大距离，作为下次条件松弛的约束
                    distance = RVOMath.det(lines[i].direction, lines[i].point - result);
                }
            }
        }
    }
}
