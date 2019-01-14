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
        // float,��ʾ����
        internal IList<KeyValuePair<float, Agent>> agentNeighbors_ = new List<KeyValuePair<float, Agent>>();
        // �����ľ�̬�ϰ���
        internal IList<KeyValuePair<float, Obstacle>> obstacleNeighbors_ = new List<KeyValuePair<float, Obstacle>>();
        internal IList<Line> orcaLines_ = new List<Line>();
        internal Vector2 position_;
        internal Vector2 prefVelocity_;
        internal Vector2 velocity_;

        internal int id_ = 0;

        // agent�ο�������ٽ�agent����
        internal int maxNeighbors_ = 0;
        // agent������ٶ�
        internal float maxSpeed_ = 0.0f;
        // ����Ϊ��agent�����ľ���
        internal float neighborDist_ = 0.0f;
        // agent�򻯳�һ��Բ��radius��ʾ�뾶
        internal float radius_ = 0.0f;
        // Ԥ��agent֮�䷢����ײ��ʱ��
        internal float timeHorizon_ = 0.0f;
        // Ԥ��agent��obstacle������ײ��ʱ��
        internal float timeHorizonObst_ = 0.0f;

        private Vector2 newVelocity_;

        #region fzy add��
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

        // һЩ����
        float arriveDistance = 0.25f;
        System.Random rnd = new System.Random();
        #endregion
        /// <summary>
        /// fzy add:����prefVelocity
        /// </summary>
        internal void setPrefVelocity()
        {
            goalVector = goal - position_v3;
            // �ٶȷ���仯
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
            // fzy modify: �ٶ�������0.0f~agentMaxSpeed
            if (goalVector.magnitude > maxSpeed_)
            {
                goalVector = goalVector.normalized * maxSpeed_;
            }

            prefVelocity_ = RVOMath.V3ToV2(goalVector);
        }

        #region ORCA ���㺯��
        /**
         * <summary>Computes the neighbors of this agent.</summary>
         */
        internal void computeNeighbors()
        {
            // ��⸽���ϰ���
            obstacleNeighbors_.Clear();
            float rangeSq = RVOMath.sqr(timeHorizonObst_ * maxSpeed_ + radius_);
            Simulator.Instance.kdTree_.computeObstacleNeighbors(this, rangeSq);

            agentNeighbors_.Clear();

            // ��⸽��Agent
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
                // fzy remark�������ǰORCA Line�Ѿ��������ϰ��������ǣ������ظ�����
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
                //����agent��obstacleֱ�ߣ����߶Σ��ľ���
                float distSqLine = RVOMath.absSq(-relativePosition1 - s * obstacleVector);

                Line line;

                // fzy remark:agent�Ѿ���obstacle�ཻ������obstacle�ߵ����
                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    /* Collision with left vertex. Ignore if non-convex. */
                    // fzy remark:�����㱻����͹���㵲ס��͹�������ڵ�obstacle�߰Ѱ��������ڵ�obstacle���ڵ���
                    if (obstacle1.convex_)
                    {
                        // Ϊʲô��(0,0)����ΪAgent�Ѿ���Obstacle��ײ��
                        line.point = new Vector2(0.0f, 0.0f);
                        line.direction = RVOMath.normalize(new Vector2(-relativePosition1.y, relativePosition1.x));
                        orcaLines_.Add(line);
                    }

                    continue;
                }
                // fzy remark:agent�Ѿ���obstacle�ཻ������obstacle�ߵ��Ҳ�
                else if (s > 1.0f && distSq2 <= radiusSq)
                {
                    /*
                     * Collision with right vertex. Ignore if non-convex or if
                     * it will be taken care of by neighboring obstacle.
                     */
                     // fzy remark:1.�����㲻���ǡ�2.͹���� �� obstacle2������һ��������ϰ����agent����нǴ���90�ȣ������ǡ�
                     // ����90�ȵ��Ȳ����ǣ��ڼ���obstacle2��ʱ���ټ��㣬�����ظ�����(��277��fzy remark)
                    if (obstacle2.convex_ && RVOMath.det(relativePosition2, obstacle2.direction_) >= 0.0f)
                    {
                        line.point = new Vector2(0.0f, 0.0f);
                        line.direction = RVOMath.normalize(new Vector2(-relativePosition2.y, relativePosition2.x));
                        orcaLines_.Add(line);
                    }

                    continue;
                }
                // fzy remark:agent�Ѿ���obstacle�ཻ������obstacle�ߵ��м�
                else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                {
                    /* Collision with obstacle segment. */
                    line.point = new Vector2(0.0f, 0.0f);
                    // fzy remark��ע�ⷽ��
                    line.direction = -obstacle1.direction_;
                    orcaLines_.Add(line);

                    continue;
                }

                /*
                 * No collision. Compute legs. When obliquely viewed, both legs
                 * can come from a single vertex. Legs extend cut-off line when
                 * non-convex vertex.
                 */
                // fzy remark:leg->��agentΪԲ�ģ�radiusΪ�뾶��Բ��Բ��Ŀ��㣨obstacle1��obstacle2������Բ��2�����ߣ����2���е�
                // ���е�ָ��Բ��Ŀ���ķ�����������Ϊleg
                Vector2 leftLegDirection, rightLegDirection;

                // fzy remark��������Բ����
                if (s < 0.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that left vertex
                     * defines velocity obstacle.
                     */
                    // fzy remark�����ڵ㲻����
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
                // ������Բ����
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
                        // ���ڵ���obstacle�ڣ�����obstacle1���෴����������ΪORCA Line
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
                        // ���ڵ�ᴩ��obstacle��ֱ��ʹ�ð��ڵ��߶ε�������
                        rightLegDirection = obstacle1.direction_;
                    }
                }

                /*
                 * Legs can never point into neighboring edge when convex
                 * vertex, take cutoff-line of neighboring edge instead. If
                 * velocity projected on "foreign" leg, no constraint is added.
                 */

                // Ѱ��velocity_����VO����߽�����ĵ�
                // ORCA Line.Point = �����
                // ORCA Line.Direction = VO����������������
                Obstacle leftNeighbor = obstacle1.previous_;

                bool isLeftLegForeign = false;
                bool isRightLegForeign = false;

                // legָ���ٽ�obstacle�߶���
                // fzy remark:agent��leftNeighbor���Ҳ�
                //��leftNeighbor������һ��������ϰ����agent����нǴ���90�ȣ�
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
                // fzy remark:ע��obstacle1��obstacle2�����Ѿ������仯
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
                    // ��Ͷ�䵽��һ��Obstacle Line������֮ǰ��Obstacle���㣬���ظ����Line��
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
                // ��Ͷ�䵽��һ��Obstacle Line������֮���Obstacle���㣬�����Ȳ���ӣ�
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

                // fzy remark��λ�ò��������ٶȲ������ķ������෴��
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
                    // w���ٶ����������߾�����λʱ���agent��other agent�����λ������
                    Vector2 w = relativeVelocity - invTimeHorizon * relativePosition;

                    /* Vector from cutoff center to relative velocity. */
                    // fzy remark:cutoff center ��Ϊ invTimeHorizon * relativePosition
                    float wLengthSq = RVOMath.absSq(w);
                    float dotProduct1 = Vector2.Dot(w, relativePosition);

                    // ����ٶ�����VO������������Բ��
                    if (dotProduct1 < 0.0f && RVOMath.sqr(dotProduct1) > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        float wLength = RVOMath.sqrt(wLengthSq);
                        Vector2 unitW = w / wLength;

                        // ����㣨����㣩λ��Բ���ϣ�u����Ϊ��Բ���ķ���
                        line.direction = new Vector2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    // ����ٶ�����VO������������Բ�����ߣ�leg����
                    else
                    {
                        /* Project on legs. */
                        float leg = RVOMath.sqrt(distSq - combinedRadiusSq);

                        // ����ʽ����0����ORCA Line�ķ�������Ͷ�䵽����
                        if (RVOMath.det(relativePosition, w) > 0.0f)
                        {
                            /* Project on left leg. */
                            // ���úϽǹ�ʽ�������ߵ�λ����
                            line.direction = new Vector2(relativePosition.x * leg - relativePosition.y * combinedRadius, 
                                relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }
                        // ���򣬽�ORCA Line�ķ�������Ͷ�䵽����
                        else
                        {
                            /* Project on right leg. */
                            // ���úϽǹ�ʽ�������ߵ�λ����
                            line.direction = -new Vector2(relativePosition.x * leg + relativePosition.y * combinedRadius, 
                                -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }

                        // ����u������ٶ�������VO��Ե���������
                        // n��u��VO�����Ե�����������
                        float dotProduct2 = Vector2.Dot(relativeVelocity, line.direction);
                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                // �Ѿ���ײ
                else
                {
                    /* Collision. Project on cut-off circle of time timeStep. */
                    // ������timeStep������Ԥ��ʱ��timeHorizon
                    float invTimeStep = 1.0f / Simulator.Instance.timeStep_;

                    /* Vector from cutoff center to relative velocity. */
                    Vector2 w = relativeVelocity - invTimeStep * relativePosition;

                    float wLength = RVOMath.abs(w);
                    Vector2 unitW = w / wLength;

                    line.direction = new Vector2(unitW.y, -unitW.x);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                // ÿ��agent���Գе�һ��ı�����Ϊ
                line.point = velocity_ + 0.5f * u;
                orcaLines_.Add(line);
            }
            #endregion

            // ʹ�ö�ά���Թ滮��⣬lineFail��ʾ���Թ滮�����Լ������
            int lineFail = linearProgram2(orcaLines_, maxSpeed_, prefVelocity_, false, ref newVelocity_);

            // ���Թ滮�����Լ������С����Լ��������δ���ȫ�����Ž⣨newVelocity_����þֲ����Ž⣩
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

                    // �������С��������
                    int i = agentNeighbors_.Count - 1;

                    while (i != 0 && distSq < agentNeighbors_[i - 1].Key)
                    {
                        agentNeighbors_[i] = agentNeighbors_[i - 1];
                        --i;
                    }

                    agentNeighbors_[i] = new KeyValuePair<float, Agent>(distSq, agent);

                    // ���¼������Χ
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

            // agent���ϰ���ľ���С��range
            if (distSq < rangeSq)
            {
                // ��obstacle����������µ�obstacle
                obstacleNeighbors_.Add(new KeyValuePair<float, Obstacle>(distSq, obstacle));

                // obstacle�����С��������
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
            // ���������ײ������transform.position != position_����Ҫ����position_���ù�����δ��ӣ�
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

            // Ӧ��ת��
            if (gpuSkinningController.playingClip.name != "Idle")
            {

                //fzy add(18.5.5):velocity_ = Vector2.zero ��
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
            // fzy remark:��(0,0)ָ��lines[lineNo].point������������lines[linesNo].direction��ͶӰ
            float dotProduct = Vector2.Dot(lines[lineNo].point, lines[lineNo].direction);
            // �б�ֵ������ORCAԼ�����Ƿ񴩹�Լ��Բ��radius/MaxSpeed���ڣ����ɶ���
            float discriminant = RVOMath.sqr(dotProduct) + RVOMath.sqr(radius) - RVOMath.absSq(lines[lineNo].point);

            // С��0����ʾlines[lineNo]������ٶ�Բ�⣨���ཻ����
            // ����������result��optVelocity.magnitude<=radius
            // ִ��linearProgram1�������ж�
            if (discriminant < 0.0f)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            // fzy remark:lines[lineNO]��Լ��Բ��radius/MaxSpeed��������������
            float sqrtDiscriminant = RVOMath.sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                // ��ĸ�ͷ���
                // lines[lineNo]��lines[i]�нǵ�sinֵ
                float denominator = RVOMath.det(lines[lineNo].direction, lines[i].direction);
                // 
                float numerator = RVOMath.det(lines[i].direction, lines[lineNo].point - lines[i].point);

                // ��ĸ����0��lines[i]��lines[linesNo]ƽ���ҷ�����ͬ��
                if (RVOMath.fabs(denominator) <= RVOMath.RVO_EPSILON)
                {
                    /* Lines lineNo and i are (almost) parallel. */
                    // ����С��0��ʾlines[i]�ĸ��������lines[lineNo]��
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                //t:lineNo��i�Ľ��㵽point�ľ���
                float t = numerator / denominator;

                // ����lineNo��i�Ľ��㣬����tLeft��tRight
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

                // lineNo��i�Ľ��㲻��Բ��
                if (tLeft > tRight)
                {
                    return false;
                }
            }

            // �����Ż�������pref�ٶȵķ������result��ֻȡ���˼�ֵ��
            if (directionOpt)
            {
                /* Optimize direction. */
                // pref�ٶ���Լ���ߵļн�С��90�ȣ�ȡ�Ҽ�ֵΪresult
                if (Vector2.Dot(optVelocity, lines[lineNo].direction) > 0.0f)
                {
                    /* Take right extreme. */
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                // pref�ٶ���Լ���ߵļнǴ��ڵ���90�ȣ�ȡ��ֵΪresult
                else
                {
                    /* Take left extreme. */
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
            }
            // �����Ż���Ѱ���ٶȱ仯��С�ĵ㣩
            else
            {
                /* Optimize closest point. */
                float t = Vector2.Dot(lines[lineNo].direction, optVelocity - lines[lineNo].point);

                // ����optVelocityͶ�䵽lineNo�ϵľ��룬����result
                if (t < tLeft)
                {
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                // optVelocity��tLeft��tRight�м�
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
        // ����ֵ���ɹ�������Լ����������
        private int linearProgram2(IList<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            // optVelocityֻ�ǹ�һ�����ٶȷ���
            if (directionOpt)
            {
                /*
                 * Optimize direction. Note that the optimization velocity is of
                 * unit length in this case.
                 */
                //result = optVelocity * radius;
                result = optVelocity.normalized * radius;
            }
            // �ٶȴ�С������MaxSpeed��
            else if(optVelocity.magnitude > radius)
            //Author Version�������˲���Ҫ�ļ��㸴�Ӷ�
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
                // ��ǰresult��ORCAԼ���ߵ���ʱ��һ�ࣨ�������Լ���ߣ������¼���result
                if (RVOMath.det(lines[i].direction, lines[i].point - result) > 0.0f)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    Vector2 tempResult = result;
                    if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                    {
                        // �Ҳ������Ž⣬����֮ǰ��result���ֲ����Ž⣩���Լ���ǰԼ���ߵ�����
                        result = tempResult;

                        // i��ʾ�Ѿ��ɹ������Թ滮Լ������
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
                // ����result�㵽lines[i]�Ĵ�ֱ����
                // result��lines[i]�����ڣ�distanceΪ��ֵ������Լ��������ֱ�ӽ�����һ��ѭ��
                // result��lines[i]�����⣬distanceΪ��ֵ��������Լ��������
                // ��result��lines[i]�ľ���С��distance����ʾlines[i]�����ǵ�ǰӰ��result����Լ����֮ǰ���и����Լ�����Ѿ����м��㣩��ֱ�ӽ�����һ��ѭ��
                if (RVOMath.det(lines[i].direction, lines[i].point - result) > distance)
                {
                    /* Result does not satisfy constraint of line i. */
                    IList<Line> projLines = new List<Line>();
                    // obstacle line Լ���������ɳ�
                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projLines.Add(lines[ii]);
                    }

                    // agent line Լ��������Ҫ�ɳ�
                    for (int j = numObstLines; j < i; ++j)
                    {
                        Line line;

                        // line[i]��line[j]�нǵ�sinֵ
                        float determinant = RVOMath.det(lines[i].direction, lines[j].direction);

                        // line[i]ƽ��line[j]����ͬ������෴��
                        if (RVOMath.fabs(determinant) <= RVOMath.RVO_EPSILON)
                        {
                            /* Line i and line j are parallel. */
                            // line[i].direction��line[j].direction��ȣ����ÿ���line[j]
                            if (Vector2.Dot(lines[i].direction, lines[j].direction) > 0.0f)
                            {
                                /* Line i and line j point in the same direction. */
                                continue;
                            }
                            // line[i].direction��line[j].direction�෴��lineΪlines[i].point��lines[j].point���е�
                            // ֻ��ƽ�вſ���ȡ�е�
                            // fzy remark��ƽ���޷��󽻵㣬ͨ���е����line.point
                            else
                            {
                                /* Line i and line j point in opposite direction. */
                                line.point = 0.5f * (lines[i].point + lines[j].point);
                            }
                        }
                        else
                        {
                            // ��lines[i]��lines[j]�Ľ�����Ϊline.point
                            // (RVOMath.det(lines[j].direction, lines[i].point - lines[j].point):lines[i].point��lines[j]�Ĵ�ֱ����
                            // determinant:lines[i]��lines[j]�нǵ�sinֵ
                            line.point = lines[i].point + 
                                (RVOMath.det(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                        }

                        line.direction = RVOMath.normalize(lines[j].direction - lines[i].direction);
                        projLines.Add(line);
                    }

                    Vector2 tempResult = result;
                    if (linearProgram2(projLines, radius, new Vector2(-lines[i].direction.y, lines[i].direction.x), true, ref result) < projLines.Count)
                    {
                        // ����������������ᷢ�������ܻ�����float���㣬����agent΢С������������
                        /*
                         * This should in principle not happen. The result is by
                         * definition already in the feasible region of this
                         * linear program. If it fails, it is due to small
                         * floating point error, and the current result is kept.
                         */
                        result = tempResult;
                    }

                    // ���������룬��Ϊ�´������ɳڵ�Լ��
                    distance = RVOMath.det(lines[i].direction, lines[i].point - result);
                }
            }
        }
    }
}
