SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[OrderSagaData](
	[Id] [uniqueidentifier] NOT NULL,
	[Originator] [varchar](50) COLLATE Latin1_General_CI_AS,
	[OriginalMessageId] [varchar](50) COLLATE Latin1_General_CI_AS,
	[PurchaseOrderNumber] [varchar](50) COLLATE Latin1_General_CI_AS NOT NULL,
	[PartnerId] [uniqueidentifier] NOT NULL,
	[ProvideBy] [datetime] NOT NULL,
 CONSTRAINT [PK_OrderSagaData] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE TABLE [dbo].[OrderSagaDataLines](
	[Id] [uniqueidentifier] NOT NULL,
	[OrderSagaDataId] [uniqueidentifier] NOT NULL,
	[ProductId] [uniqueidentifier] NOT NULL,
	[Quantity] [float] NOT NULL,
	[AuthorizedQuantity] [float] NOT NULL CONSTRAINT [DF_OrderSagaDataLines_AuthorizedQuantity]  DEFAULT ((0)),
 CONSTRAINT [PK_OrderSagaDataLines] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[OrderSagaDataLines]  WITH CHECK ADD  CONSTRAINT [FK_OrderSagaDataLines_OrderSagaData] FOREIGN KEY([OrderSagaDataId])
REFERENCES [dbo].[OrderSagaData] ([Id])
GO