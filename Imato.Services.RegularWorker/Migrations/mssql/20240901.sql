alter proc dbo.SetWorker
	@id int,
	@name varchar(255),
	@appName varchar(512),
	@active bit,
	@host varchar(255),
	@settings varchar(2000),
	@executed datetime, 
	@error varchar(max) = ''
as 
begin 
	set nocount on;

	declare @hosts int = 0,
		@activeHosts int = 0;

	begin transaction; 

	select @hosts = count(1), @activeHosts = sum(iif(active = 1, 1, 0))
		from dbo.Workers 
		where name = @name 
			and date >= dateadd(millisecond, -60000, getdate())
			and @host != host;

	set @hosts = @hosts + 1;
	set @activeHosts = isnull(@activeHosts, 0);

	update dbo.Workers 
		set date = getdate(), 
			active = @active,
			hosts = @hosts,
			error = @error,
			executed = @executed
		where (@id > 0 and id = @id) 
			or (host = @host 
				and name = @name 
				and appName = @appName); 
				
	if @@ROWCOUNT = 0 
		insert into dbo.Workers 
			(name, host, appName, date, settings, active, hosts, error, executed) 
		values 
			(@name, @host, @appName, getdate(), @settings, @active, @hosts, @error, @executed); 
			
	select top 1 *, @activeHosts as activeHosts
		from dbo.Workers 
		where (@id > 0 and id = @id) 
			or (host = @host 
				and name = @name 
				and appName = @appName); 

	commit transaction;
end;