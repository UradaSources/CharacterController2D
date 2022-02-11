using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// A simple aabb character controller, 6th February 2022, Urada

public static class Tools
{
	public static Vector2Int Sign(Vector2 vector)
	{
		return new Vector2Int(Math.Sign(vector.x), Math.Sign(vector.y));
	}
}

public class CharacterController2D : MonoBehaviour
{
	private const float MinRayAdvance = 0.02f;

	// necessary component
	private BoxCollider2D m_collider;

	// member and param
	public Vector2 position { get; set; }
	public Vector2 velocity { get; set; }

	public LayerMask m_platformMask = 0;

	public bool m_useGravity = true;
	public float m_gravityScale = 1.0f;

	private UnityEvent m_event;

	// update position based on speed
	private void UpdatePosition()
	{
		if (this.velocity != Vector2.zero)
		{
			var trend = Tools.Sign(this.velocity);
			if (trend.x != 0 && this.GetHorizontalCollision(trend.x))
			{
				this.velocity = new Vector2(0, this.velocity.y);
			}
			if (trend.y != 0 && this.GetVerticalCollision(trend.y))
			{
				this.velocity = new Vector2(this.velocity.x, 0);
			}
			this.position += this.velocity * Time.fixedDeltaTime;
		}

		transform.position = this.position;
	}

	private bool m_collidedAbove, m_collidedBelow;
	private bool m_collidedRight, m_collidedLeft;

	// eq this.m_verticalCollision[this.ToIndex(-1)];
	public bool grounded
	{
		get { return m_collidedBelow; }
	}

	public bool touchWallRight
	{
		get{ return m_collidedRight; }
	}
	public bool touchWallLeft
	{
		get { return m_collidedLeft; }
	}

	// check localScale.x to call correct right touchWall version
	public bool touchWallFront
	{
		get
		{
			return this.GetHorizontalCollision(Math.Sign(transform.localScale.x));
		}
	}
	public bool touchWallBack
	{
		get
		{
			return this.GetHorizontalCollision(-Math.Sign(transform.localScale.x));
		}
	}

	private void ResetCollisionState()
	{
		m_collidedAbove = false;
		m_collidedBelow = false;

		m_collidedRight = false;
		m_collidedLeft = false;
	}

	public ref bool GetHorizontalCollision(int sign)
	{
		if (sign > 0) return ref this.m_collidedRight;
		else return ref this.m_collidedLeft;
	}
	public ref bool GetVerticalCollision(int sign)
	{
		if (sign > 0) return ref this.m_collidedAbove;
		else return ref this.m_collidedBelow;
	}

	public int GetHorizontalCollisionSign()
	{
		if (this.m_collidedRight) return 1;
		else if (this.m_collidedLeft) return -1;
		else return 0;
	}
	public int GetVerticalCollisionSign()
	{
		if (this.m_collidedAbove) return 1;
		else if (this.m_collidedBelow) return -1;
		else return 0;
	}

	// attempt to check for collision and snap to the collision point
	private void CheckVerticalCollision(int sign)
	{
		if (sign == 0) return;

		// calculate the advance of boxcast based on speed
		float advance = Mathf.Max(MinRayAdvance, Mathf.Abs(this.velocity.y) * Time.fixedDeltaTime);

		// calculated parameters
		var boxPosition = this.position;
		boxPosition.y += sign * advance / 2;

		var boxSize = new Vector2(m_collider.size.x * 0.95f, advance);

		// get collision flag
		ref bool collisionFlag = ref this.GetVerticalCollision(sign);

		// check collision and snap
		var hit = Physics2D.BoxCast(boxPosition, boxSize, 0, sign * Vector2.up, m_collider.size.y / 2, m_platformMask);
		if (hit.collider != null && hit.normal.y != 0)
		{
			// make it step
			if (Mathf.Abs(this.velocity.y) > 0.0f)
			{
				this.velocity = new Vector2(this.velocity.x, 0);
			}

			// snap to hit point
			float snapPosition = hit.point.y - sign * (m_collider.size.y / 2);
			this.position = new Vector2(this.position.x, snapPosition);

			// update collision flag
			collisionFlag = true;
		}
		else 
		{
			collisionFlag = false;			
		}
	}
	private void CheckHorizontalCollision(int sign)
	{
		if (sign == 0) return;

		// calculate the advance of boxcast based on speed
		float advance = Mathf.Max(MinRayAdvance, Mathf.Abs(this.velocity.x) * Time.fixedDeltaTime);

		// calculated parameters
		var rayPosition = this.position;
		rayPosition.x += sign * advance / 2;

		var raySize = new Vector2(advance, m_collider.size.y * 0.95f);

		// get collision flag
		ref bool collisionFlag = ref this.GetHorizontalCollision(sign);

		// check collision and snap
		var hit = Physics2D.BoxCast(rayPosition, raySize, 0, sign * Vector2.right, m_collider.size.x / 2, m_platformMask);
		if (hit.collider != null && hit.normal.x != 0)
		{
			// make it step
			if (Mathf.Abs(this.velocity.x) > 0.0f)
			{
				this.velocity = new Vector2(0, this.velocity.y);
			}

			// snap to hit point
			float snapPosition = hit.point.x - sign * (m_collider.size.x / 2);
			this.position = new Vector2(snapPosition, this.position.y);

			// update snap flag
			collisionFlag = true;
		}
		else
		{ 
			collisionFlag = false;			
		}
	}

	private void CheckCollision()
	{
		// get the current movement trend and reset the collision flag
		var trend = Tools.Sign(this.velocity);

		this.ResetCollisionState();

		// when the vertical trend is 0, keep checking the ground
		this.CheckVerticalCollision(trend.y > 0 ? 1 : -1);
		this.CheckHorizontalCollision(trend.x);
	}

	public float CalculateJumpSpeed(float height)
	{
		float gravity = Physics2D.gravity.magnitude * this.m_gravityScale;
		return Mathf.Sqrt(2.0f * gravity * height);
	}

	private float m_jumpTimer = 0;
	private float m_jumpInterval = 0.1f;

	// simple control
	public void SimpleMove(int direction, float speed, float acceleration, float midairFactor = 0.2f, bool turn = true)
	{
		direction = Math.Sign(direction);

		if (direction != 0 && turn)
		{
			transform.localScale = new Vector3(direction, transform.localScale.y);
		}

		float delta = acceleration * Time.fixedDeltaTime;
		float targetSpeed = direction * speed;

		if (!this.grounded)
		{ 
			delta *= midairFactor;
		}

		Vector2 velocity = this.velocity;
		velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, delta);
		this.velocity = velocity;
	}
	public bool TrySimpleJump(float height)
	{
		if (m_jumpTimer <= 0 && this.grounded)
		{
			float force = this.CalculateJumpSpeed(height);
			this.velocity = new Vector2(this.velocity.x, force);

			// reset timer
			m_jumpTimer = m_jumpInterval;

			return true;
		}
		return false;
	}

	void Start()
	{
		this.position = transform.position;
		m_collider = GetComponent<BoxCollider2D>();

		
	}

	void FixedUpdate()
	{
		// check and handle collision
		this.CheckCollision();

		// apply gravity
		if (this.m_useGravity && !this.grounded)
		{
			Vector2 velocity = this.velocity;
			velocity += Vector2.down * 10.0f * Time.fixedDeltaTime;
			this.velocity = velocity;
		}

		// update position and jump timer
		this.UpdatePosition();

		if (m_jumpTimer > 0) m_jumpTimer -= Time.fixedDeltaTime;
	}
}
