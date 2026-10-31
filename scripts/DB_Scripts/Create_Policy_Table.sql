USE [PolicyDb]
GO

/****** Object:  Table [dbo].[Policy]    Script Date: 10/26/2026 12:37:29 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Policy](
	[PolicyID] [int] IDENTITY(1,1) NOT NULL,
	[PolicyNumber] [nvarchar](12) NOT NULL,
	[TermNumber] [int] NOT NULL,
	[Lob] [nvarchar](50) NOT NULL,
	[EffectiveDate] [date] NOT NULL,
	[ExpirationDate] [date] NOT NULL,
	[PolicyHolderName] [nvarchar](100) NOT NULL,
	[RequestID] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Policy] PRIMARY KEY CLUSTERED 
(
	[PolicyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Policy_PolicyNumber_TermNumber] UNIQUE NONCLUSTERED 
(
	[PolicyNumber] ASC,
	[TermNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Policy_RequestID] UNIQUE NONCLUSTERED 
(
	[RequestID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Policy]  WITH CHECK ADD  CONSTRAINT [FK_Policy_LineOfBusiness] FOREIGN KEY([Lob])
REFERENCES [dbo].[LineOfBusiness] ([LOB])
GO

ALTER TABLE [dbo].[Policy] CHECK CONSTRAINT [FK_Policy_LineOfBusiness]
GO

ALTER TABLE [dbo].[Policy]  WITH CHECK ADD  CONSTRAINT [CK_Policy_CoverageDates] CHECK  (([ExpirationDate]>[EffectiveDate]))
GO

ALTER TABLE [dbo].[Policy] CHECK CONSTRAINT [CK_Policy_CoverageDates]
GO

ALTER TABLE [dbo].[Policy]  WITH CHECK ADD  CONSTRAINT [CK_Policy_TermNumber] CHECK  (([TermNumber]>=(1)))
GO

ALTER TABLE [dbo].[Policy] CHECK CONSTRAINT [CK_Policy_TermNumber]
GO


