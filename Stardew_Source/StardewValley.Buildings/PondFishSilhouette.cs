using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;

namespace StardewValley.Buildings;

public class PondFishSilhouette
{
	public Vector2 position;

	protected FishPond _pond;

	protected Object _fishObject;

	protected Vector2 _velocity = Vector2.Zero;

	protected float nextDart;

	protected bool _upRight;

	protected float _age;

	protected float _wiggleTimer;

	protected float _sinkAmount = 1f;

	protected float _randomOffset;

	protected bool _flipped;

	public PondFishSilhouette(FishPond pond)
	{
		_pond = pond;
		_fishObject = _pond.GetFishObject();
		if (_fishObject.HasContextTag("fish_upright"))
		{
			_upRight = true;
		}
		position = (_pond.GetCenterTile() + new Vector2(0.5f, 0.5f)) * 64f;
		_age = 0f;
		_randomOffset = Utility.Lerp(0f, 500f, (float)Game1.random.NextDouble());
		ResetDartTime();
	}

	public void ResetDartTime()
	{
		nextDart = Utility.Lerp(20f, 40f, (float)Game1.random.NextDouble());
	}

	public void Draw(SpriteBatch b)
	{
		float angle = (float)Math.PI / 4f;
		if (_upRight)
		{
			angle = 0f;
		}
		SpriteEffects effect = SpriteEffects.None;
		angle += (float)Math.Sin(_wiggleTimer + _randomOffset) * 2f * (float)Math.PI / 180f;
		if (_velocity.Y < 0f)
		{
			angle -= (float)Math.PI / 18f;
		}
		if (_velocity.Y > 0f)
		{
			angle += (float)Math.PI / 18f;
		}
		if (_flipped)
		{
			effect = SpriteEffects.FlipHorizontally;
			angle *= -1f;
		}
		float draw_scale = Utility.Lerp(0.75f, 0.65f, Utility.Clamp(_sinkAmount, 0f, 1f));
		draw_scale *= Utility.Lerp(1f, 0.75f, (float)_pond.currentOccupants.Value / 10f);
		Vector2 draw_position = position;
		draw_position.Y += (float)Math.Sin(_age * 2f + _randomOffset) * 5f;
		draw_position.Y += (int)(_sinkAmount * 4f);
		float transparency = Utility.Lerp(0.25f, 0.15f, Utility.Clamp(_sinkAmount, 0f, 1f));
		Vector2 origin = new Vector2(8f, 8f);
		ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(_fishObject.QualifiedItemId);
		b.Draw(itemData.GetTexture(), Game1.GlobalToLocal(Game1.viewport, draw_position), itemData.GetSourceRect(0, null), Color.Black * transparency, angle, origin, 4f * draw_scale, effect, position.Y / 10000f + 1E-06f);
	}

	public bool IsMoving()
	{
		return _velocity.LengthSquared() > 0f;
	}

	public void Update(float time)
	{
		nextDart -= time;
		_age += time;
		_wiggleTimer += time;
		if (nextDart <= 0f || (nextDart <= 0.5f && Game1.random.NextDouble() < 0.10000000149011612))
		{
			ResetDartTime();
			int direction = Game1.random.Next(0, 2) * 2 - 1;
			if (direction < 0)
			{
				_flipped = true;
			}
			else
			{
				_flipped = false;
			}
			_velocity = new Vector2((float)direction * Utility.Lerp(50f, 100f, (float)Game1.random.NextDouble()), Utility.Lerp(-50f, 50f, (float)Game1.random.NextDouble()));
		}
		bool moving = _velocity.LengthSquared() > 0f;
		if (moving)
		{
			_wiggleTimer += time * 30f;
			_sinkAmount = Utility.MoveTowards(_sinkAmount, 0f, 2f * time);
		}
		else
		{
			_sinkAmount = Utility.MoveTowards(_sinkAmount, 1f, 1f * time);
		}
		position += _velocity * time;
		for (int i = 0; i < _pond.GetFishSilhouettes().Count; i++)
		{
			PondFishSilhouette other_silhouette = _pond.GetFishSilhouettes()[i];
			if (other_silhouette == this)
			{
				continue;
			}
			float push_amount = 30f;
			float push_other_amount = 30f;
			if (IsMoving())
			{
				push_amount = 0f;
			}
			if (other_silhouette.IsMoving())
			{
				push_other_amount = 0f;
			}
			if (Math.Abs(other_silhouette.position.X - position.X) < 32f)
			{
				if (other_silhouette.position.X > position.X)
				{
					other_silhouette.position.X += push_other_amount * time;
					position.X += (0f - push_amount) * time;
				}
				else
				{
					other_silhouette.position.X -= push_other_amount * time;
					position.X += push_amount * time;
				}
			}
			if (Math.Abs(other_silhouette.position.Y - position.Y) < 32f)
			{
				if (other_silhouette.position.Y > position.Y)
				{
					other_silhouette.position.Y += push_other_amount * time;
					position.Y += -1f * time;
				}
				else
				{
					other_silhouette.position.Y -= push_other_amount * time;
					position.Y += 1f * time;
				}
			}
		}
		_velocity.X = Utility.MoveTowards(_velocity.X, 0f, 50f * time);
		_velocity.Y = Utility.MoveTowards(_velocity.Y, 0f, 20f * time);
		float border_width = 1.3f;
		if (position.X > ((float)(_pond.tileX.Value + _pond.tilesWide.Value) - border_width) * 64f)
		{
			position.X = ((float)(_pond.tileX.Value + _pond.tilesWide.Value) - border_width) * 64f;
			_velocity.X *= -1f;
			if (moving && (Game1.random.NextDouble() < 0.25 || Math.Abs(_velocity.X) > 30f))
			{
				_flipped = !_flipped;
			}
		}
		if (position.X < ((float)_pond.tileX.Value + border_width) * 64f)
		{
			position.X = ((float)_pond.tileX.Value + border_width) * 64f;
			_velocity.X *= -1f;
			if (moving && (Game1.random.NextDouble() < 0.25 || Math.Abs(_velocity.X) > 30f))
			{
				_flipped = !_flipped;
			}
		}
		if (position.Y > ((float)(_pond.tileY.Value + _pond.tilesHigh.Value) - border_width) * 64f)
		{
			position.Y = ((float)(_pond.tileY.Value + _pond.tilesHigh.Value) - border_width) * 64f;
			_velocity.Y *= -1f;
		}
		if (position.Y < ((float)_pond.tileY.Value + border_width) * 64f)
		{
			position.Y = ((float)_pond.tileY.Value + border_width) * 64f;
			_velocity.Y *= -1f;
		}
	}
}
