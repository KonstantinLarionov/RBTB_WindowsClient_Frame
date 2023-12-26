using System.Collections.Generic;
using BinanceMapper.Spot.UserStream;
using BinanceMapper.Spot.UserStream.Data;
using BinanceMapper.Spot.UserStream.Events;
using BinanceMapper.Spot.Websocket;
using BinanceMapper.Spot.Websocket.Data;
using BinanceMapper.Spot.WebSocket.Events;

using WebSocketSharp;

namespace RBTB_WindowsClient_Frame.Watchers
{
	public class OrdersWatcher
	{
		public delegate void ActivateOrder(long id);
		public event ActivateOrder ActivateOrderEvent;

		private List<long> ActiveOrder = new List<long>();
		private WebSocket _socket;

		private WebsocketApiV1HandlerComposition SpotPublic;
		private UserStreamApiV1HandlerComposition SpotUser;

		public OrdersWatcher(string privateUrl)
		{
			_socket = new WebSocket(privateUrl);
			SpotPublic = new WebsocketApiV1HandlerComposition( new WebSocketApiV1HandlerFactory() );
			SpotUser = new UserStreamApiV1HandlerComposition( new UserStreamApiV1HandlerFactory() );

			_socket.OnMessage += _socket_OnMessage;
			_socket.OnError += _socket_OnError;
			_socket.OnClose += _socket_OnClose;
			_socket.Connect();
		}

		private void _socket_OnClose( object sender, CloseEventArgs e )
		{
		}

		private void _socket_OnError( object sender, ErrorEventArgs e )
		{
		}

		private void _socket_OnMessage( object sender, MessageEventArgs e )
		{
			DefaultEvent def_event = null;

			try
			{
				def_event = SpotPublic.HandleDefaultEvent( e.Data );


				if ( def_event != null )
				{
					if ( def_event.EventType == WebsocketEventType.ExecutionReport )
					{
						var us_event = SpotUser.HandleUserStreamEvent( e.Data );
						var ex_or = (OrderExecutionReport)us_event;
						if ( (ex_or.OrderStatusType == OrderStatus.Filled || ex_or.OrderStatusType == OrderStatus.PartiallyFilled)
							&& ActiveOrder.Contains( ex_or.OrderID ) )
						{
							ActivateOrderEvent?.Invoke(ex_or.OrderID);
							ActiveOrder.Remove( ex_or.OrderID );
						}
					}
				}
			}
			catch
			{
			}
		}

		public void AddActiveOrder( long id ) => ActiveOrder.Add( id );
		
	}
}
